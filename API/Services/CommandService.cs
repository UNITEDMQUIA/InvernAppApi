using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using InvernApp.Api.Exceptions;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface ICommandService
{
    Task<IReadOnlyList<CommandResponse>> GetAsync(
        string deviceId, CommandStatus? status, CancellationToken cancellationToken);

    Task<CommandResponse> AcknowledgeAsync(
        string deviceId, long commandId, AcknowledgeCommandRequest request, CancellationToken cancellationToken);
}

public class CommandService : ICommandService
{
    private const int MaxHistory = 100;
    private const int MaxAckMessageLength = 200;
    private const int MaxEventMessageLength = 500;

    private readonly InvernAppDbContext _db;
    private readonly ILogger<CommandService> _logger;

    public CommandService(InvernAppDbContext db, ILogger<CommandService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CommandResponse>> GetAsync(
        string deviceId,
        CommandStatus? status,
        CancellationToken cancellationToken)
    {
        var canonicalId = await ResolveDeviceIdAsync(deviceId, cancellationToken);
        var query = _db.Commands.AsNoTracking().Where(c => c.DeviceId == canonicalId);

        List<Command> items;

        if (status is null or CommandStatus.Pending)
        {
            var now = DateTime.UtcNow;

            items = await query
                .Where(c => c.Status == CommandStatus.Pending && c.ExpiresAt > now)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToListAsync(cancellationToken);

            if (items.Count > 0)
            {
                _logger.LogInformation(
                    "{DeviceId} consultó sus comandos: {Count} pendiente(s)", canonicalId, items.Count);
            }
        }
        else
        {
            var wanted = status.Value;

            items = await query
                .Where(c => c.Status == wanted)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(MaxHistory)
                .ToListAsync(cancellationToken);
        }

        return items.Select(ToResponse).ToList();
    }

    public async Task<CommandResponse> AcknowledgeAsync(
        string deviceId,
        long commandId,
        AcknowledgeCommandRequest request,
        CancellationToken cancellationToken)
    {
        var canonicalId = await ResolveDeviceIdAsync(deviceId, cancellationToken);

        var command = await _db.Commands
            .FirstOrDefaultAsync(c => c.Id == commandId && c.DeviceId == canonicalId, cancellationToken)
            ?? throw new NotFoundException(
                $"El comando {commandId} no existe para el dispositivo '{canonicalId}'.");

        var executed = request.Executed!.Value;

        if (command.Status is CommandStatus.Acknowledged or CommandStatus.Rejected)
        {
            var wasExecuted = command.Status == CommandStatus.Acknowledged;

            if (wasExecuted == executed)
            {
                _logger.LogInformation(
                    "Confirmación repetida del comando #{CommandId} de {DeviceId}; sin cambios",
                    commandId, canonicalId);
                return ToResponse(command);
            }

            throw new ConflictException(
                $"El comando {commandId} ya fue registrado como " +
                $"{(wasExecuted ? "ejecutado" : "rechazado")}; no se puede cambiar su resultado.");
        }

        var now = DateTime.UtcNow;
        var late = command.Status == CommandStatus.Expired;
        var (actuator, on) = CommandMapping.Describe(command.Type);

        var ackMessage = BuildAckMessage(request.Message, late);

        command.AcknowledgedAt = now;
        command.AckMessage = ackMessage;

        if (executed)
        {
            command.Status = CommandStatus.Acknowledged;

            var state = await _db.ActuatorStates
                .FirstOrDefaultAsync(a => a.DeviceId == canonicalId && a.Actuator == actuator, cancellationToken);

            var changed = false;

            if (state is null)
            {
                _db.ActuatorStates.Add(new ActuatorState
                {
                    DeviceId = canonicalId,
                    Actuator = actuator,
                    IsOn = on,
                    ChangedAt = now
                });
                changed = true;
            }
            else if (state.IsOn != on)
            {
                state.IsOn = on;
                state.ChangedAt = now; 
                changed = true;
            }

            if (changed)
            {
                var label = ActionLabel(actuator, on);
                var message = string.IsNullOrWhiteSpace(command.Reason)
                    ? label
                    : $"{label}. {command.Reason}";

                _db.Events.Add(new Event
                {
                    DeviceId = canonicalId,
                    Type = ToEventType(actuator, on),
                    Severity = EventSeverity.Info,
                    Message = Truncate(message, MaxEventMessageLength),
                    CommandId = command.Id,
                    CreatedAt = now
                });
            }

            _logger.LogInformation(
                "Comando #{CommandId} {Type} ejecutado por {DeviceId} (cambió el estado: {Changed}{Late})",
                command.Id, command.Type, canonicalId, changed, late ? ", confirmación tardía" : string.Empty);
        }
        else
        {
            command.Status = CommandStatus.Rejected;

            var message = $"Comando {command.Type} rechazado por el dispositivo." +
                          (ackMessage is null ? string.Empty : $" {ackMessage}");

            _db.Events.Add(new Event
            {
                DeviceId = canonicalId,
                Type = EventType.CommandRejected,
                Severity = EventSeverity.Warning,
                Message = Truncate(message, MaxEventMessageLength),
                CommandId = command.Id,
                CreatedAt = now
            });

            _logger.LogWarning(
                "Comando #{CommandId} {Type} rechazado por {DeviceId}: {Message}",
                command.Id, command.Type, canonicalId, ackMessage);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(command);
    }

    /// <summary>Devuelve el DeviceId registrado o lanza 404 si el dispositivo no existe.</summary>
    private async Task<string> ResolveDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        var requested = deviceId.Trim();

        var canonical = await _db.Devices
            .AsNoTracking()
            .Where(d => d.DeviceId == requested)
            .Select(d => d.DeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        return canonical ?? throw new NotFoundException($"El dispositivo '{requested}' no está registrado.");
    }

    private static string? BuildAckMessage(string? message, bool late)
    {
        var text = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        if (late)
        {
            text = text is null ? "Confirmación tardía." : $"Confirmación tardía. {text}";
        }

        return text is null ? null : Truncate(text, MaxAckMessageLength);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";

    private static string ActionLabel(ActuatorType actuator, bool on) => (actuator, on) switch
    {
        (ActuatorType.Pump, true) => "Riego iniciado",
        (ActuatorType.Pump, false) => "Riego detenido",
        (ActuatorType.Fan, true) => "Ventilador activado",
        (ActuatorType.Fan, false) => "Ventilador desactivado",
        (ActuatorType.Light, true) => "Iluminación activada",
        (ActuatorType.Light, false) => "Iluminación desactivada",
        _ => throw new ArgumentOutOfRangeException(nameof(actuator), actuator, "Actuador desconocido.")
    };

    private static EventType ToEventType(ActuatorType actuator, bool on) => (actuator, on) switch
    {
        (ActuatorType.Pump, true) => EventType.WateringStarted,
        (ActuatorType.Pump, false) => EventType.WateringStopped,
        (ActuatorType.Fan, true) => EventType.FanStarted,
        (ActuatorType.Fan, false) => EventType.FanStopped,
        (ActuatorType.Light, true) => EventType.LightStarted,
        (ActuatorType.Light, false) => EventType.LightStopped,
        _ => throw new ArgumentOutOfRangeException(nameof(actuator), actuator, "Actuador desconocido.")
    };

    private static CommandResponse ToResponse(Command c) =>
        new(c.Id,
            c.DeviceId,
            c.Type,
            c.Status,
            c.Reason,
            c.AckMessage,
            c.CreatedAt,
            c.ExpiresAt,
            c.AcknowledgedAt);
}
using System.Globalization;
using InvernApp.Api.Data;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IAutomationService
{
    /// <summary>
    /// Evalúa las reglas de riego, ventilación e iluminación con la medición recibida,
    /// genera los comandos necesarios y registra las alertas.
    /// </summary>
    Task EvaluateAsync(Measurement measurement, CancellationToken cancellationToken);
}

public class AutomationService : IAutomationService
{
    private const int ConfigurationId = 1;

    private static readonly TimeSpan CommandLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MaxMeasurementAge = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PumpRestartCooldown = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan CriticalAlertCooldown = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MissingDataAlertCooldown = TimeSpan.FromMinutes(30);

    private const string CriticalAlertPrefix = "Temperatura crítica";
    private const string MissingSoilDataAlertPrefix = "Riego habilitado sin dato";

    private readonly InvernAppDbContext _db;
    private readonly ILogger<AutomationService> _logger;

    public AutomationService(InvernAppDbContext db, ILogger<AutomationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EvaluateAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var deviceId = measurement.DeviceId;

        if (now - measurement.Timestamp > MaxMeasurementAge)
        {
            _logger.LogInformation(
                "Automatización omitida: la medición {DeviceId} #{SequenceNumber} tiene más de {Minutes} min de antigüedad",
                deviceId, measurement.SequenceNumber, MaxMeasurementAge.TotalMinutes);
            return;
        }

        var config = await _db.Configurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ConfigurationId, cancellationToken);

        if (config is null)
        {
            _logger.LogWarning("Automatización omitida: no existe la fila de configuración");
            return;
        }

        var states = await _db.ActuatorStates
            .Where(a => a.DeviceId == deviceId)
            .ToDictionaryAsync(a => a.Actuator, cancellationToken);

        foreach (var actuator in Enum.GetValues<ActuatorType>())
        {
            if (states.ContainsKey(actuator))
            {
                continue;
            }

            var created = new ActuatorState
            {
                DeviceId = deviceId,
                Actuator = actuator,
                IsOn = false,
                ChangedAt = now
            };
            _db.ActuatorStates.Add(created);
            states[actuator] = created;
        }

        var pending = await _db.Commands
            .Where(c => c.DeviceId == deviceId && c.Status == CommandStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var expired in pending.Where(c => c.ExpiresAt <= now).ToList())
        {
            expired.Status = CommandStatus.Expired;
            expired.AckMessage = "Caducó sin ser confirmado.";
            pending.Remove(expired);
        }

        var decisions = new (ActuatorType Actuator, Decision Decision)[]
        {
            (ActuatorType.Pump, DecidePump(config, measurement, states[ActuatorType.Pump], now)),
            (ActuatorType.Fan, DecideFan(config, measurement)),
            (ActuatorType.Light, DecideLight(config, measurement))
        };

        var newCommands = new List<Command>();

        foreach (var (actuator, decision) in decisions)
        {
            var command = Apply(deviceId, states[actuator], decision, pending, now);
            if (command is null)
            {
                continue;
            }

            newCommands.Add(command);

            if (decision.IsMaxTimeStop)
            {
                _db.Events.Add(new Event
                {
                    DeviceId = deviceId,
                    Type = EventType.Alert,
                    Severity = EventSeverity.Warning,
                    Message = $"Bomba detenida: {decision.Reason}",
                    Command = command,
                    CreatedAt = now
                });
            }
        }

        if (measurement.Temperature >= config.CriticalTemperature)
        {
            await AddAlertIfNotRecentAsync(
                deviceId,
                CriticalAlertPrefix,
                $"{CriticalAlertPrefix}: {N(measurement.Temperature)} °C >= {N(config.CriticalTemperature)} °C. " +
                "Se solicita el ventilador.",
                CriticalAlertCooldown,
                now,
                cancellationToken);
        }

        if (config.WateringEnabled && measurement.SoilHumidity is null)
        {
            await AddAlertIfNotRecentAsync(
                deviceId,
                MissingSoilDataAlertPrefix,
                $"{MissingSoilDataAlertPrefix} de humedad del suelo; la bomba permanece apagada.",
                MissingDataAlertCooldown,
                now,
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var command in newCommands)
        {
            _logger.LogInformation(
                "Comando generado #{CommandId} {Type} para {DeviceId}: {Reason}",
                command.Id, command.Type, deviceId, command.Reason);
        }
    }

    private static Decision DecidePump(
        Configuration config, Measurement measurement, ActuatorState state, DateTime now)
    {
        if (!config.WateringEnabled)
        {
            return Decision.Off("Riego automático deshabilitado");
        }

        if (measurement.SoilHumidity is not double soil)
        {
            return Decision.Off("Sin dato de humedad del suelo");
        }

        var simulated = measurement.IsSoilHumiditySimulated ? " (dato simulado)" : string.Empty;

        if (state.IsOn)
        {
            var onSeconds = (now - state.ChangedAt).TotalSeconds;
            if (onSeconds >= config.MaximumWateringSeconds)
            {
                return Decision.Off(
                    $"Tiempo máximo de riego alcanzado ({onSeconds:0} s >= {config.MaximumWateringSeconds} s)",
                    isMaxTimeStop: true);
            }
        }

        if (soil >= config.SoilHumidityTarget)
        {
            return Decision.Off(
                $"Humedad del suelo {N(soil)} % >= objetivo {N(config.SoilHumidityTarget)} %{simulated}");
        }

        if (soil < config.SoilHumidityMinimum)
        {
            if (!state.IsOn && now - state.ChangedAt < PumpRestartCooldown)
            {
                return Decision.Maintain;
            }

            return Decision.On(
                $"Humedad del suelo {N(soil)} % < mínimo {N(config.SoilHumidityMinimum)} %{simulated}");
        }

        return Decision.Maintain;
    }

    private static Decision DecideFan(Configuration config, Measurement measurement)
    {
        var t = measurement.Temperature;

        if (t >= config.CriticalTemperature)
        {
            return Decision.On(
                $"Temperatura crítica: {N(t)} °C >= {N(config.CriticalTemperature)} °C");
        }

        if (!config.VentilationEnabled)
        {
            return Decision.Off("Ventilación automática deshabilitada");
        }

        if (t > config.MaximumTemperature)
        {
            return Decision.On($"Temperatura {N(t)} °C > máximo {N(config.MaximumTemperature)} °C");
        }

        var offLimit = config.MaximumTemperature - config.TemperatureHysteresis;
        if (t <= offLimit)
        {
            return Decision.Off($"Temperatura {N(t)} °C <= {N(offLimit)} °C");
        }

        return Decision.Maintain;
    }

    private static Decision DecideLight(Configuration config, Measurement measurement)
    {
        var l = measurement.Luminosity;

        if (!config.LightingEnabled)
        {
            return Decision.Off("Iluminación automática deshabilitada");
        }

        if (l < config.MinimumLuminosity)
        {
            return Decision.On($"Luminosidad {N(l)} % < mínima {N(config.MinimumLuminosity)} %");
        }

        if (l >= config.TargetLuminosity)
        {
            return Decision.Off($"Luminosidad {N(l)} % >= objetivo {N(config.TargetLuminosity)} %");
        }

        return Decision.Maintain;
    }

    /// <summary>
    /// Convierte una decisión en un comando, sin duplicar pendientes y reemplazando
    /// los pendientes que ya contradicen la decisión. Devuelve el comando nuevo, si lo hay.
    /// </summary>
    private Command? Apply(
        string deviceId,
        ActuatorState state,
        Decision decision,
        List<Command> pending,
        DateTime now)
    {
        var pendingCommand = pending.FirstOrDefault(c => Describe(c.Type).Actuator == state.Actuator);
        var effective = pendingCommand is null ? state.IsOn : Describe(pendingCommand.Type).On;
        var target = decision.Target ?? effective;

        if (pendingCommand is not null && target != effective)
        {
            pendingCommand.Status = CommandStatus.Expired;
            pendingCommand.AckMessage = "Reemplazado por una decisión más reciente.";
            pending.Remove(pendingCommand);
            effective = state.IsOn;
        }

        if (target == effective)
        {
            return null;
        }

        var command = new Command
        {
            DeviceId = deviceId,
            Type = ToCommandType(state.Actuator, target),
            Status = CommandStatus.Pending,
            Reason = decision.Reason,
            CreatedAt = now,
            ExpiresAt = now + CommandLifetime
        };

        _db.Commands.Add(command);
        return command;
    }

    private async Task AddAlertIfNotRecentAsync(
        string deviceId,
        string prefix,
        string message,
        TimeSpan cooldown,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var since = now - cooldown;

        var alreadyRaised = await _db.Events.AnyAsync(
            e => e.DeviceId == deviceId
                 && e.Type == EventType.Alert
                 && e.CreatedAt >= since
                 && e.Message.StartsWith(prefix),
            cancellationToken);

        if (alreadyRaised)
        {
            return;
        }

        _db.Events.Add(new Event
        {
            DeviceId = deviceId,
            Type = EventType.Alert,
            Severity = EventSeverity.Warning,
            Message = message,
            CreatedAt = now
        });

        _logger.LogWarning("Alerta en {DeviceId}: {Message}", deviceId, message);
    }
    private static (ActuatorType Actuator, bool On) Describe(CommandType type) => type switch
    {
        CommandType.TURN_PUMP_ON => (ActuatorType.Pump, true),
        CommandType.TURN_PUMP_OFF => (ActuatorType.Pump, false),
        CommandType.TURN_FAN_ON => (ActuatorType.Fan, true),
        CommandType.TURN_FAN_OFF => (ActuatorType.Fan, false),
        CommandType.TURN_LIGHT_ON => (ActuatorType.Light, true),
        CommandType.TURN_LIGHT_OFF => (ActuatorType.Light, false),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de comando desconocido.")
    };

    private static CommandType ToCommandType(ActuatorType actuator, bool on) => (actuator, on) switch
    {
        (ActuatorType.Pump, true) => CommandType.TURN_PUMP_ON,
        (ActuatorType.Pump, false) => CommandType.TURN_PUMP_OFF,
        (ActuatorType.Fan, true) => CommandType.TURN_FAN_ON,
        (ActuatorType.Fan, false) => CommandType.TURN_FAN_OFF,
        (ActuatorType.Light, true) => CommandType.TURN_LIGHT_ON,
        (ActuatorType.Light, false) => CommandType.TURN_LIGHT_OFF,
        _ => throw new ArgumentOutOfRangeException(nameof(actuator), actuator, "Actuador desconocido.")
    };

    /// <summary>
    /// Número con un decimal y punto, sin depender de la cultura del equipo.
    /// </summary>
    private static string N(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>
    /// Resultado de evaluar una regla: encender, apagar o mantener el estado.
    /// </summary>
    private sealed record Decision(bool? Target, string Reason, bool IsMaxTimeStop = false)
    {
        public static Decision Maintain { get; } = new(null, string.Empty);

        public static Decision On(string reason) => new(true, reason);

        public static Decision Off(string reason, bool isMaxTimeStop = false) =>
            new(false, reason, isMaxTimeStop);
    }
}
using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using InvernApp.Api.Exceptions;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IDeviceService
{
    Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<DeviceResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
}

public class DeviceService : IDeviceService
{
    private readonly InvernAppDbContext _db;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(InvernAppDbContext db, ILogger<DeviceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DeviceResponse> CreateAsync(
        CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var deviceId = request.DeviceId.Trim();
        var name = request.Name.Trim();

        if (await _db.Devices.AnyAsync(d => d.DeviceId == deviceId, cancellationToken))
        {
            _logger.LogWarning("Registro rechazado: el dispositivo {DeviceId} ya existe", deviceId);
            throw new ConflictException($"Ya existe un dispositivo con deviceId '{deviceId}'.");
        }

        var now = DateTime.UtcNow;

        var device = new Device
        {
            DeviceId = deviceId,
            Name = name,
            Status = DeviceStatus.Registered,
            RegisteredAt = now
        };

        // Un estado por actuador, todos apagados al registrar.
        foreach (var actuator in Enum.GetValues<ActuatorType>())
        {
            device.ActuatorStates.Add(new ActuatorState
            {
                DeviceId = deviceId,
                Actuator = actuator,
                IsOn = false,
                ChangedAt = now
            });
        }

        _db.Devices.Add(device);

        _db.Events.Add(new Event
        {
            DeviceId = deviceId,
            Type = EventType.DeviceRegistered,
            Severity = EventSeverity.Info,
            Message = $"Dispositivo '{deviceId}' registrado.",
            CreatedAt = now
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Dos peticiones simultáneas: la segunda la rechaza el índice único.
            _logger.LogWarning(ex, "Registro rechazado por índice único: dispositivo {DeviceId}", deviceId);
            throw new ConflictException($"Ya existe un dispositivo con deviceId '{deviceId}'.");
        }

        _logger.LogInformation(
            "Dispositivo registrado: {DeviceId} (Id {Id})", device.DeviceId, device.Id);

        return ToResponse(device);
    }

    public async Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var devices = await _db.Devices
            .AsNoTracking()
            .OrderBy(d => d.DeviceId)
            .ToListAsync(cancellationToken);

        return devices.Select(ToResponse).ToList();
    }

    public async Task<DeviceResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var device = await _db.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return device is null ? null : ToResponse(device);
    }

    private static DeviceResponse ToResponse(Device device) =>
        new(device.Id,
            device.DeviceId,
            device.Name,
            device.Status,
            device.RegisteredAt,
            device.LastSeenAt);
}
using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using InvernApp.Api.Exceptions;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IMeasurementService
{
    Task<MeasurementResponse> CreateAsync(CreateMeasurementRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementResponse>> GetAsync(
        string? deviceId, DateTime? from, DateTime? to, int limit, CancellationToken cancellationToken);

    Task<MeasurementResponse?> GetLatestAsync(string? deviceId, CancellationToken cancellationToken);
}

public class MeasurementService : IMeasurementService
{
    private const int MaxLimit = 1000;

    private static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    private readonly InvernAppDbContext _db;
    private readonly ILogger<MeasurementService> _logger;

    public MeasurementService(InvernAppDbContext db, ILogger<MeasurementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MeasurementResponse> CreateAsync(
        CreateMeasurementRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var timestamp = UtcDate.ToUtc(request.Timestamp!.Value);
        var sequenceNumber = request.SequenceNumber!.Value;

        if (timestamp > now + MaxFutureSkew)
        {
            throw new BadRequestException(
                "El timestamp está más de 5 minutos en el futuro. Revise el reloj (NTP) del dispositivo.");
        }

        if (timestamp < now - MaxAge)
        {
            throw new BadRequestException(
                "El timestamp tiene más de 30 días de antigüedad. Revise el reloj (NTP) del dispositivo.");
        }

        if (request.SoilHumidity is null && request.SoilHumiditySimulated)
        {
            throw new BadRequestException(
                "soilHumiditySimulated no puede ser true cuando soilHumidity es nulo.");
        }

        var requestedDeviceId = request.DeviceId.Trim();

        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == requestedDeviceId, cancellationToken)
            ?? throw new NotFoundException($"El dispositivo '{requestedDeviceId}' no está registrado.");

        var deviceId = device.DeviceId;

        if (await _db.Measurements.AnyAsync(
                m => m.DeviceId == deviceId && m.SequenceNumber == sequenceNumber,
                cancellationToken))
        {
            _logger.LogWarning(
                "Medición duplicada: {DeviceId} secuencia {SequenceNumber}", deviceId, sequenceNumber);
            throw new ConflictException(
                $"Ya existe la medición {sequenceNumber} del dispositivo '{deviceId}'.");
        }

        var measurement = new Measurement
        {
            DeviceId = deviceId,
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            ReceivedAt = now,
            Temperature = request.Temperature!.Value,
            AirHumidity = request.AirHumidity!.Value,
            SoilHumidity = request.SoilHumidity,
            IsSoilHumiditySimulated = request.SoilHumiditySimulated,
            Luminosity = request.Luminosity!.Value
        };

        _db.Measurements.Add(measurement);

        if (device.Status == DeviceStatus.Offline)
        {
            _db.Events.Add(new Event
            {
                DeviceId = deviceId,
                Type = EventType.CommunicationRestored,
                Severity = EventSeverity.Info,
                Message = $"Comunicación restablecida con '{deviceId}'.",
                CreatedAt = now
            });
        }

        device.Status = DeviceStatus.Online;
        device.LastSeenAt = now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            _logger.LogWarning(
                ex, "Medición duplicada (índice único): {DeviceId} secuencia {SequenceNumber}",
                deviceId, sequenceNumber);
            throw new ConflictException(
                $"Ya existe la medición {sequenceNumber} del dispositivo '{deviceId}'.");
        }

        _logger.LogInformation(
            "Medición recibida: {DeviceId} #{SequenceNumber} T={Temperature} HR={AirHumidity} " +
            "Suelo={SoilHumidity} (simulado: {Simulated}) L={Luminosity}",
            deviceId, sequenceNumber, measurement.Temperature, measurement.AirHumidity,
            measurement.SoilHumidity, measurement.IsSoilHumiditySimulated, measurement.Luminosity);

        return ToResponse(measurement);
    }

    public async Task<IReadOnlyList<MeasurementResponse>> GetAsync(
        string? deviceId,
        DateTime? from,
        DateTime? to,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit < 1 || limit > MaxLimit)
        {
            throw new BadRequestException($"El parámetro limit debe estar entre 1 y {MaxLimit}.");
        }

        var fromUtc = from.HasValue ? UtcDate.ToUtc(from.Value) : (DateTime?)null;
        var toUtc = to.HasValue ? UtcDate.ToUtc(to.Value) : (DateTime?)null;

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
        {
            throw new BadRequestException("El parámetro from no puede ser posterior a to.");
        }

        var query = _db.Measurements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var canonicalId = await ResolveDeviceIdAsync(deviceId, cancellationToken);
            query = query.Where(m => m.DeviceId == canonicalId);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(m => m.Timestamp >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(m => m.Timestamp <= toUtc.Value);
        }

        var items = await query
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    public async Task<MeasurementResponse?> GetLatestAsync(
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var query = _db.Measurements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var canonicalId = await ResolveDeviceIdAsync(deviceId, cancellationToken);
            query = query.Where(m => m.DeviceId == canonicalId);
        }

        var latest = await query
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? null : ToResponse(latest);
    }

    /// <summary>
    /// Devuelve el DeviceId registrado o lanza 404 si el dispositivo no existe.
    /// </summary>
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

    private static MeasurementResponse ToResponse(Measurement m) =>
        new(m.Id,
            m.DeviceId,
            m.SequenceNumber,
            m.Timestamp,
            m.ReceivedAt,
            m.Temperature,
            m.AirHumidity,
            m.SoilHumidity,
            m.IsSoilHumiditySimulated,
            m.Luminosity);
}
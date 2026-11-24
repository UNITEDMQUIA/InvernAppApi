using System.Globalization;
using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IConfigurationService
{
    Task<ConfigurationResponse> GetAsync(CancellationToken cancellationToken);
    Task<ConfigurationResponse> UpdateAsync(ConfigurationRequest request, CancellationToken cancellationToken);
}

public class ConfigurationService : IConfigurationService
{
    private const int ConfigurationId = 1;
    private const int MaxEventMessageLength = 500;

    private readonly InvernAppDbContext _db;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(InvernAppDbContext db, ILogger<ConfigurationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ConfigurationResponse> GetAsync(CancellationToken cancellationToken)
    {
        var config = await _db.Configurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ConfigurationId, cancellationToken)
            ?? throw MissingRow();

        return ToResponse(config);
    }

    public async Task<ConfigurationResponse> UpdateAsync(
        ConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var config = await _db.Configurations
            .FirstOrDefaultAsync(c => c.Id == ConfigurationId, cancellationToken)
            ?? throw MissingRow();

        var changes = new List<string>();

        void Set<T>(string name, T current, T next, Action<T> assign)
        {
            if (EqualityComparer<T>.Default.Equals(current, next))
            {
                return;
            }

            changes.Add($"{name}: {Format(current)} → {Format(next)}");
            assign(next);
        }

        Set(nameof(config.SoilHumidityMinimum), config.SoilHumidityMinimum,
            request.SoilHumidityMinimum!.Value, v => config.SoilHumidityMinimum = v);
        Set(nameof(config.SoilHumidityTarget), config.SoilHumidityTarget,
            request.SoilHumidityTarget!.Value, v => config.SoilHumidityTarget = v);
        Set(nameof(config.MaximumWateringSeconds), config.MaximumWateringSeconds,
            request.MaximumWateringSeconds!.Value, v => config.MaximumWateringSeconds = v);
        Set(nameof(config.MaximumTemperature), config.MaximumTemperature,
            request.MaximumTemperature!.Value, v => config.MaximumTemperature = v);
        Set(nameof(config.TemperatureHysteresis), config.TemperatureHysteresis,
            request.TemperatureHysteresis!.Value, v => config.TemperatureHysteresis = v);
        Set(nameof(config.CriticalTemperature), config.CriticalTemperature,
            request.CriticalTemperature!.Value, v => config.CriticalTemperature = v);
        Set(nameof(config.MinimumLuminosity), config.MinimumLuminosity,
            request.MinimumLuminosity!.Value, v => config.MinimumLuminosity = v);
        Set(nameof(config.TargetLuminosity), config.TargetLuminosity,
            request.TargetLuminosity!.Value, v => config.TargetLuminosity = v);
        Set(nameof(config.WateringEnabled), config.WateringEnabled,
            request.WateringEnabled!.Value, v => config.WateringEnabled = v);
        Set(nameof(config.VentilationEnabled), config.VentilationEnabled,
            request.VentilationEnabled!.Value, v => config.VentilationEnabled = v);
        Set(nameof(config.LightingEnabled), config.LightingEnabled,
            request.LightingEnabled!.Value, v => config.LightingEnabled = v);

        if (changes.Count == 0)
        {
            _logger.LogInformation("Actualización de configuración sin cambios");
            return ToResponse(config);
        }

        var now = DateTime.UtcNow;
        config.UpdatedAt = now;

        var message = "Configuración actualizada. " + string.Join("; ", changes);
        if (message.Length > MaxEventMessageLength)
        {
            message = message[..(MaxEventMessageLength - 3)] + "...";
        }

        _db.Events.Add(new Event
        {
            DeviceId = null,
            Type = EventType.ConfigurationChanged,
            Severity = EventSeverity.Info,
            Message = message,
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Configuración actualizada: {Changes}", string.Join("; ", changes));

        return ToResponse(config);
    }

    private static InvalidOperationException MissingRow() =>
        new("No existe la fila de configuración (Id = 1). Aplique las migraciones de la base de datos.");

    private static string Format<T>(T value) => value switch
    {
        bool b => b ? "true" : "false",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? string.Empty
    };

    private static ConfigurationResponse ToResponse(Configuration c) =>
        new(c.SoilHumidityMinimum,
            c.SoilHumidityTarget,
            c.MaximumWateringSeconds,
            c.MaximumTemperature,
            c.TemperatureHysteresis,
            c.CriticalTemperature,
            c.MinimumLuminosity,
            c.TargetLuminosity,
            c.WateringEnabled,
            c.VentilationEnabled,
            c.LightingEnabled,
            c.UpdatedAt);
}
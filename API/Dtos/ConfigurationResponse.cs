namespace InvernApp.Api.Dtos;

/// <summary>
/// Configuración de automatización vigente.
/// </summary>
public record ConfigurationResponse(
    double SoilHumidityMinimum,
    double SoilHumidityTarget,
    int MaximumWateringSeconds,
    double MaximumTemperature,
    double TemperatureHysteresis,
    double CriticalTemperature,
    double MinimumLuminosity,
    double TargetLuminosity,
    bool WateringEnabled,
    bool VentilationEnabled,
    bool LightingEnabled,
    DateTime UpdatedAt);
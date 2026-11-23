namespace InvernApp.Api.Models;

/// <summary>
/// Parámetros de automatización. Para el prototipo existe una única fila (Id = 1).
/// Los valores por defecto son parámetros iniciales de laboratorio, no valores agronómicos universales.
/// </summary>
public class Configuration
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Riego (histéresis: ON si < Minimum, OFF si >= Target)
    /// </summary>
    public double SoilHumidityMinimum { get; set; } = 30;

    /// <summary>
    /// Humedad del suelo objetivo (histéresis: ON si < Minimum, OFF si >= Target)
    /// </summary>
    public double SoilHumidityTarget { get; set; } = 45;

    /// <summary>
    /// Protección: la bomba se apaga al superar este tiempo de funcionamiento continuo.
    /// </summary>
    public int MaximumWateringSeconds { get; set; } = 30;

    /// <summary>
    /// Ventilación (ON si T > Maximum, OFF si T <= Maximum - Hysteresis)
    /// </summary>
    public double MaximumTemperature { get; set; } = 30;
    public double TemperatureHysteresis { get; set; } = 2;

    /// <summary>
    /// Por encima de este valor el ESP32 activa el ventilador sin depender de la API.
    /// </summary>
    public double CriticalTemperature { get; set; } = 35;

    /// <summary>
    /// Iluminación (ON si L < Minimum, OFF si L >= Target)
    /// </summary>
    public double MinimumLuminosity { get; set; } = 30;

    /// <summary>
    /// Luminosidad objetivo (ON si L < Minimum, OFF si L >= Target)
    /// </summary>
    public double TargetLuminosity { get; set; } = 45;

    /// <summary>
    /// Habilitación de cada automatización
    /// </summary>
    public bool WateringEnabled { get; set; }

    /// <summary>
    /// Ventilación habilitada
    /// </summary>
    public bool VentilationEnabled { get; set; } = true;

    /// <summary>
    /// Habilitación de la iluminación. Si es false, el ESP32 no enciende la luz aunque L < Minimum.
    /// </summary>
    public bool LightingEnabled { get; set; } = true;

    /// <summary>
    /// Fecha de creación de la fila (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Configuración completa de automatización. PUT reemplaza todos los valores,
/// por eso todos los campos son obligatorios.
/// </summary>
public class ConfigurationRequest : IValidatableObject
{
    /// <summary>
    /// La bomba se activa si la humedad del suelo baja de este valor (%).
    /// </summary>
    [Required(ErrorMessage = "El campo soilHumidityMinimum es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo soilHumidityMinimum debe estar entre 0 y 100 %.")]
    public double? SoilHumidityMinimum { get; set; }

    /// <summary>
    /// La bomba se apaga cuando la humedad del suelo alcanza este valor (%).
    /// </summary>
    [Required(ErrorMessage = "El campo soilHumidityTarget es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo soilHumidityTarget debe estar entre 0 y 100 %.")]
    public double? SoilHumidityTarget { get; set; }

    /// <summary>
    /// Tiempo máximo continuo de la bomba encendida (segundos). Protección ante fallos de sensor.
    /// </summary>
    [Required(ErrorMessage = "El campo maximumWateringSeconds es obligatorio.")]
    [Range(1, 600, ErrorMessage = "El campo maximumWateringSeconds debe estar entre 1 y 600 segundos.")]
    public int? MaximumWateringSeconds { get; set; }

    /// <summary>
    /// El ventilador se activa por encima de esta temperatura (°C).
    /// </summary>
    [Required(ErrorMessage = "El campo maximumTemperature es obligatorio.")]
    [Range(0.0, 60.0, ErrorMessage = "El campo maximumTemperature debe estar entre 0 y 60 °C.")]
    public double? MaximumTemperature { get; set; }

    /// <summary>
    /// El ventilador se apaga cuando T &lt;= maximumTemperature - temperatureHysteresis (°C).
    /// </summary>
    [Required(ErrorMessage = "El campo temperatureHysteresis es obligatorio.")]
    [Range(0.1, 20.0, ErrorMessage = "El campo temperatureHysteresis debe estar entre 0,1 y 20 °C.")]
    public double? TemperatureHysteresis { get; set; }

    /// <summary>
    /// Temperatura crítica: el ESP32 activa el ventilador localmente sin depender de la API (°C).
    /// </summary>
    [Required(ErrorMessage = "El campo criticalTemperature es obligatorio.")]
    [Range(0.0, 80.0, ErrorMessage = "El campo criticalTemperature debe estar entre 0 y 80 °C.")]
    public double? CriticalTemperature { get; set; }

    /// <summary>
    /// Los LEDs se activan si la luminosidad relativa baja de este valor (%).
    /// </summary>
    [Required(ErrorMessage = "El campo minimumLuminosity es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo minimumLuminosity debe estar entre 0 y 100 %.")]
    public double? MinimumLuminosity { get; set; }

    /// <summary>
    /// Los LEDs se apagan cuando la luminosidad relativa alcanza este valor (%).
    /// </summary>
    [Required(ErrorMessage = "El campo targetLuminosity es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo targetLuminosity debe estar entre 0 y 100 %.")]
    public double? TargetLuminosity { get; set; }

    [Required(ErrorMessage = "El campo wateringEnabled es obligatorio.")]
    public bool? WateringEnabled { get; set; }

    [Required(ErrorMessage = "El campo ventilationEnabled es obligatorio.")]
    public bool? VentilationEnabled { get; set; }

    [Required(ErrorMessage = "El campo lightingEnabled es obligatorio.")]
    public bool? LightingEnabled { get; set; }

    /// <summary>
    /// Reglas de coherencia entre campos. ASP.NET Core solo las ejecuta cuando los
    /// campos individuales ya pasaron sus validaciones, así que los valores no son nulos aquí.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SoilHumidityMinimum!.Value >= SoilHumidityTarget!.Value)
        {
            yield return new ValidationResult(
                "soilHumidityMinimum debe ser menor que soilHumidityTarget.",
                new[] { nameof(SoilHumidityMinimum), nameof(SoilHumidityTarget) });
        }

        if (CriticalTemperature!.Value <= MaximumTemperature!.Value)
        {
            yield return new ValidationResult(
                "criticalTemperature debe ser mayor que maximumTemperature.",
                new[] { nameof(CriticalTemperature), nameof(MaximumTemperature) });
        }

        if (MinimumLuminosity!.Value >= TargetLuminosity!.Value)
        {
            yield return new ValidationResult(
                "minimumLuminosity debe ser menor que targetLuminosity.",
                new[] { nameof(MinimumLuminosity), nameof(TargetLuminosity) });
        }
    }
}
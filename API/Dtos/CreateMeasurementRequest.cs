using System.ComponentModel.DataAnnotations;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Medición enviada por el ESP32.
/// </summary>
public class CreateMeasurementRequest
{
    /// <summary>
    /// Identificador del dispositivo registrado (ej. INVERN-01).
    /// </summary>
    [Required(ErrorMessage = "El campo deviceId es obligatorio.")]
    [StringLength(50, ErrorMessage = "El campo deviceId no puede superar 50 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$",
        ErrorMessage = "El campo deviceId solo admite letras, números, guion (-) y guion bajo (_), sin espacios.")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Contador creciente del ESP32. Junto con deviceId identifica la medición.
    /// </summary>
    [Required(ErrorMessage = "El campo sequenceNumber es obligatorio.")]
    [Range(0.0, long.MaxValue, ErrorMessage = "El campo sequenceNumber debe ser mayor o igual a 0.")]
    public long? SequenceNumber { get; set; }

    /// <summary>
    /// Hora de la medición. Se recomienda UTC con sufijo Z. Sin zona horaria se interpreta como UTC.
    /// </summary>
    [Required(ErrorMessage = "El campo timestamp es obligatorio.")]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Temperatura en °C (DHT22).
    /// </summary>
    [Required(ErrorMessage = "El campo temperature es obligatorio.")]
    [Range(-40.0, 80.0, ErrorMessage = "El campo temperature debe estar entre -40 y 80 °C.")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Humedad relativa del aire en % (DHT22).
    /// </summary>
    [Required(ErrorMessage = "El campo airHumidity es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo airHumidity debe estar entre 0 y 100 %.")]
    public double? AirHumidity { get; set; }

    /// <summary>
    /// Humedad del suelo en % (escala relativa). Nulo si no hay dato.
    /// </summary>
    [Range(0.0, 100.0, ErrorMessage = "El campo soilHumidity debe estar entre 0 y 100 %.")]
    public double? SoilHumidity { get; set; }

    /// <summary>
    /// True si soilHumidity es un valor simulado y no una lectura de un sensor físico.
    /// </summary>
    public bool SoilHumiditySimulated { get; set; }

    /// <summary>
    /// Luminosidad relativa en % (LDR). No son lux.
    /// </summary>
    [Required(ErrorMessage = "El campo luminosity es obligatorio.")]
    [Range(0.0, 100.0, ErrorMessage = "El campo luminosity debe estar entre 0 y 100 %.")]
    public double? Luminosity { get; set; }
}
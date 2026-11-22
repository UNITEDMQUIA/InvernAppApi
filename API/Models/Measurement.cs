namespace InvernApp.Api.Models;

/// <summary>
/// Una lectura enviada por el ESP32. Identificada lógicamente por DeviceId + SequenceNumber.
/// </summary>
public class Measurement
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Identificador lógico del ESP32, por ejemplo "INVERN-01". Único.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Contador creciente del ESP32; junto con DeviceId evita duplicados.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Hora de la medición según el dispositivo (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Hora en que la API recibió la medición (UTC).
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Temperatura en °C (DHT22).
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Humedad relativa del aire en % (DHT22).
    /// </summary>
    public double AirHumidity { get; set; }

    /// <summary>
    /// Humedad del suelo en % (escala relativa calibrada). Nula si no hay dato.
    /// </summary>
    public double? SoilHumidity { get; set; }

    /// <summary>
    /// True si SoilHumidity es un valor simulado (no proviene de un sensor físico).
    /// </summary>
    public bool IsSoilHumiditySimulated { get; set; }

    /// <summary>
    /// Luminosidad relativa en % (LDR). No son lux.
    /// </summary>
    public double Luminosity { get; set; }

    /// <summary>
    /// Dispositivo al que pertenece la medición.
    /// </summary>
    public Device Device { get; set; } = null!;
}
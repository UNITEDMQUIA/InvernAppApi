namespace InvernApp.Api.Models;

/// <summary>
/// Orden pendiente (o ya procesada) para un ESP32.
/// </summary>
public class Command
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// Identificador del dispositivo
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de orden.
    /// </summary>
    public CommandType Type { get; set; }

    /// <summary>
    /// Estado de la orden.
    /// </summary>
    public CommandStatus Status { get; set; } = CommandStatus.Pending;

    /// <summary>
    /// Por qué se generó, por ejemplo "SoilHumidity 25 &lt; 30".
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Mensaje del ESP32 al confirmar (por ejemplo el motivo de un rechazo).
    /// </summary>
    public string? AckMessage { get; set; }

    /// <summary>
    /// Fecha de creación de la orden (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Después de esta hora el comando ya no se entrega al ESP32.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Fecha de confirmación de la orden (UTC). Nula si no se ha confirmado.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Servicio al que pertenece la orden
    /// </summary>
    public Device Device { get; set; } = null!;
}
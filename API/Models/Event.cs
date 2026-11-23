namespace InvernApp.Api.Models;

/// <summary>
/// Registro de un hecho relevante del sistema.
/// </summary>
public class Event
{
    /// <summary>
    /// Identificador único
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Dispositivo asociado. Nulo para eventos del sistema (ej. cambio de configuración).
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Tipo de evento.
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// Severidad de evento.
    /// </summary>
    public EventSeverity Severity { get; set; } = EventSeverity.Info;

    /// <summary>
    /// Mensaje
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Comando relacionado, si el evento proviene de uno (trazabilidad).
    /// </summary>
    public long? CommandId { get; set; }

    /// <summary>
    /// Fecha de creación del evento (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Servicio de la API al que pertenece el evento. Nulo si es un evento del sistema.
    /// </summary>
    public Device? Device { get; set; }

    /// <summary>
    /// Comando relacionado, si el evento proviene de uno (trazabilidad).
    /// </summary>
    public Command? Command { get; set; }
}
namespace InvernApp.Api.Models;

/// <summary>
/// Estado actual de un actuador de un dispositivo. Una fila por (dispositivo, actuador).
/// </summary>
public class ActuatorState
{
    /// <summary>
    /// Identificiador único
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador del dispositivo
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de actuador
    /// </summary>
    public ActuatorType Actuator { get; set; }

    /// <summary>
    /// Esta activo
    /// </summary>
    public bool IsOn { get; set; }

    /// <summary>
    /// Cuándo cambió por última vez de estado (UTC).
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Servicio al que pertenece el actuador
    /// </summary>
    public Device Device { get; set; } = null!;
}
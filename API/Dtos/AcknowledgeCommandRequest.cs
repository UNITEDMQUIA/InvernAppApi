using System.ComponentModel.DataAnnotations;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Confirmación que envía el ESP32 tras procesar un comando.
/// </summary>
public class AcknowledgeCommandRequest
{
    /// <summary>
    /// True si el actuador ejecutó la orden; false si el dispositivo la rechazó.
    /// </summary>
    [Required(ErrorMessage = "El campo executed es obligatorio.")]
    public bool? Executed { get; set; }

    /// <summary>
    /// Mensaje opcional del dispositivo (por ejemplo, el motivo de un rechazo).
    /// </summary>
    [StringLength(200, ErrorMessage = "El campo message no puede superar 200 caracteres.")]
    public string? Message { get; set; }
}
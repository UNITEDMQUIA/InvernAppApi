using System.ComponentModel.DataAnnotations;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Datos para registrar un dispositivo.
/// </summary>
public class CreateDeviceRequest
{
    /// <summary>
    /// Identificador lógico del ESP32 (letras, números, guion y guion bajo). Ej.: INVERN-01.
    /// </summary>
    [Required(ErrorMessage = "El campo deviceId es obligatorio.")]
    [StringLength(50, ErrorMessage = "El campo deviceId no puede superar 50 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$",
        ErrorMessage = "El campo deviceId solo admite letras, números, guion (-) y guion bajo (_), sin espacios.")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo del dispositivo.
    /// </summary>
    [Required(ErrorMessage = "El campo name es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo name no puede superar 100 caracteres.")]
    public string Name { get; set; } = string.Empty;
}
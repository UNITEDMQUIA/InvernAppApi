using API.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace InvernApp.Api.Models;

/// <summary>
/// Un ESP32 registrado en el sistema.
/// </summary>
public class Device
{
    /// <summary>
    /// Identificador único.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador lógico del ESP32, por ejemplo "INVERN-01". Único.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado registrado
    /// </summary>
    public DeviceStatus Status { get; set; } = DeviceStatus.Registered;

    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Última vez que el dispositivo se comunicó con la API.
    /// </summary>
    public DateTime? LastSeenAt { get; set; }

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<ActuatorState> ActuatorStates { get; set; } = new List<ActuatorState>();
    public ICollection<Command> Commands { get; set; } = new List<Command>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
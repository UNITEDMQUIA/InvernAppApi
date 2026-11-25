using InvernApp.Api.Models;

namespace InvernApp.Api.Services;

/// <summary>
/// Conversión entre comandos y (actuador, encendido/apagado).
/// </summary>
internal static class CommandMapping
{
    public static (ActuatorType Actuator, bool On) Describe(CommandType type) => type switch
    {
        CommandType.TURN_PUMP_ON => (ActuatorType.Pump, true),
        CommandType.TURN_PUMP_OFF => (ActuatorType.Pump, false),
        CommandType.TURN_FAN_ON => (ActuatorType.Fan, true),
        CommandType.TURN_FAN_OFF => (ActuatorType.Fan, false),
        CommandType.TURN_LIGHT_ON => (ActuatorType.Light, true),
        CommandType.TURN_LIGHT_OFF => (ActuatorType.Light, false),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de comando desconocido.")
    };

    public static CommandType ToCommandType(ActuatorType actuator, bool on) => (actuator, on) switch
    {
        (ActuatorType.Pump, true) => CommandType.TURN_PUMP_ON,
        (ActuatorType.Pump, false) => CommandType.TURN_PUMP_OFF,
        (ActuatorType.Fan, true) => CommandType.TURN_FAN_ON,
        (ActuatorType.Fan, false) => CommandType.TURN_FAN_OFF,
        (ActuatorType.Light, true) => CommandType.TURN_LIGHT_ON,
        (ActuatorType.Light, false) => CommandType.TURN_LIGHT_OFF,
        _ => throw new ArgumentOutOfRangeException(nameof(actuator), actuator, "Actuador desconocido.")
    };
}
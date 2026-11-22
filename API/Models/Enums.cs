namespace InvernApp.Api.Models;

/// <summary>
/// Estado de comunicación del dispositivo.
/// </summary>
public enum DeviceStatus
{
    Registered,
    Online,
    Offline
}

/// <summary>
/// Actuadores controlados por el ESP32.
/// </summary>
public enum ActuatorType
{
    Pump,
    Fan,
    Light
}

/// <summary>
/// Órdenes que la API puede enviar al ESP32.
/// </summary>
public enum CommandType
{
    TURN_PUMP_ON,
    TURN_PUMP_OFF,
    TURN_FAN_ON,
    TURN_FAN_OFF,
    TURN_LIGHT_ON,
    TURN_LIGHT_OFF
}

/// <summary>
/// Ciclo de vida de un comando.
/// </summary>
public enum CommandStatus
{
    Pending,
    Acknowledged,
    Rejected,
    Expired
}

/// <summary>
/// Severidad de un evento.
/// </summary>
public enum EventSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Tipo de evento a registrar.
/// </summary>
public enum EventType
{
    DeviceRegistered,
    WateringStarted,
    WateringStopped,
    FanStarted,
    FanStopped,
    LightStarted,
    LightStopped,
    CommunicationLost,
    CommunicationRestored,
    ConfigurationChanged,
    CommandRejected,
    Alert,
    Error
}
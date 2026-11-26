using InvernApp.Api.Models;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Estado actual de un actuador.
/// </summary>
public record ActuatorStateResponse(
    ActuatorType Actuator,
    bool IsOn,
    DateTime ChangedAt);
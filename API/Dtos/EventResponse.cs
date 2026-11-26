using InvernApp.Api.Models;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Representación pública de un evento.
/// </summary>
public record EventResponse(
    long Id,
    string? DeviceId,
    EventType Type,
    EventSeverity Severity,
    string Message,
    long? CommandId,
    DateTime CreatedAt);
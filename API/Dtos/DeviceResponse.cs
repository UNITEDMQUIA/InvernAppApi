using InvernApp.Api.Models;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Representación pública de un dispositivo.
/// </summary>
public record DeviceResponse(
    int Id,
    string DeviceId,
    string Name,
    DeviceStatus Status,
    DateTime RegisteredAt,
    DateTime? LastSeenAt);
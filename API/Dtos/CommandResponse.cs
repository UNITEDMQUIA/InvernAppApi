using InvernApp.Api.Models;

namespace InvernApp.Api.Dtos;

/// <summary>
/// Representación pública de un comando.
/// </summary>
public record CommandResponse(
    long Id,
    string DeviceId,
    CommandType Type,
    CommandStatus Status,
    string? Reason,
    string? AckMessage,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? AcknowledgedAt);
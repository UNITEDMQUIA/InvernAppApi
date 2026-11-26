using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using InvernApp.Api.Exceptions;
using InvernApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventResponse>> GetAsync(
        string? deviceId, EventType? type, int limit, CancellationToken cancellationToken);
}

public class EventService : IEventService
{
    private const int MaxLimit = 500;

    private readonly InvernAppDbContext _db;

    public EventService(InvernAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EventResponse>> GetAsync(
        string? deviceId,
        EventType? type,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit < 1 || limit > MaxLimit)
        {
            throw new BadRequestException($"El parámetro limit debe estar entre 1 y {MaxLimit}.");
        }

        var query = _db.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var canonicalId = await _db.ResolveDeviceIdAsync(deviceId, cancellationToken);
            query = query.Where(e => e.DeviceId == canonicalId);
        }

        if (type.HasValue)
        {
            var wanted = type.Value;
            query = query.Where(e => e.Type == wanted);
        }

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items.Select(ToResponse).ToList();
    }

    private static EventResponse ToResponse(Event e) =>
        new(e.Id, e.DeviceId, e.Type, e.Severity, e.Message, e.CommandId, e.CreatedAt);
}
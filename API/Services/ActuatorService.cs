using InvernApp.Api.Data;
using InvernApp.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Services;

public interface IActuatorService
{
    Task<IReadOnlyList<ActuatorStateResponse>> GetByDeviceAsync(
        string deviceId, CancellationToken cancellationToken);
}

public class ActuatorService : IActuatorService
{
    private readonly InvernAppDbContext _db;

    public ActuatorService(InvernAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ActuatorStateResponse>> GetByDeviceAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        var canonicalId = await _db.ResolveDeviceIdAsync(deviceId, cancellationToken);

        var states = await _db.ActuatorStates
            .AsNoTracking()
            .Where(a => a.DeviceId == canonicalId)
            .ToListAsync(cancellationToken);

        return states
            .OrderBy(a => a.Actuator)
            .Select(a => new ActuatorStateResponse(a.Actuator, a.IsOn, a.ChangedAt))
            .ToList();
    }
}
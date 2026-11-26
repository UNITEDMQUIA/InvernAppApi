using InvernApp.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Data;

public static class InvernAppDbContextExtensions
{
    /// <summary>
    /// Devuelve el DeviceId tal como está registrado o lanza 404 si el dispositivo no existe.
    /// </summary>
    public static async Task<string> ResolveDeviceIdAsync(
        this InvernAppDbContext db,
        string deviceId,
        CancellationToken cancellationToken)
    {
        var requested = deviceId.Trim();

        var canonical = await db.Devices
            .AsNoTracking()
            .Where(d => d.DeviceId == requested)
            .Select(d => d.DeviceId)
            .FirstOrDefaultAsync(cancellationToken);

        return canonical ?? throw new NotFoundException($"El dispositivo '{requested}' no está registrado.");
    }
}
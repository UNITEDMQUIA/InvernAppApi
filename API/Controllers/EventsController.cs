using InvernApp.Api.Dtos;
using InvernApp.Api.Models;
using InvernApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Lista eventos, los más recientes primero.
    /// Filtros opcionales: deviceId, type y limit (1-500, por defecto 50).
    /// Sin deviceId incluye también los eventos del sistema (por ejemplo, cambios de configuración).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> Get(
        [FromQuery] string? deviceId,
        [FromQuery] EventType? type,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var events = await _eventService.GetAsync(deviceId, type, limit, cancellationToken);
        return Ok(events);
    }
}
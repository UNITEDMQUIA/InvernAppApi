using InvernApp.Api.Dtos;
using InvernApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class MeasurementsController : ControllerBase
{
    private readonly IMeasurementService _measurementService;

    public MeasurementsController(IMeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    /// <summary>
    /// Recibe una medición del ESP32.
    /// </summary>
    [HttpPost("measurements")]
    [ProducesResponseType(typeof(MeasurementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeasurementResponse>> Create(
        CreateMeasurementRequest request,
        CancellationToken cancellationToken)
    {
        var measurement = await _measurementService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, measurement);
    }

    /// <summary>
    /// Lista mediciones, las más recientes primero.
    /// Filtros opcionales: deviceId, from, to (fechas ISO 8601, recomendado UTC con Z) y limit (1-1000, por defecto 100).
    /// </summary>
    [HttpGet("measurements")]
    [ProducesResponseType(typeof(IReadOnlyList<MeasurementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MeasurementResponse>>> GetAll(
        [FromQuery] string? deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await _measurementService.GetAsync(deviceId, from, to, limit, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Devuelve la medición más reciente, opcionalmente de un dispositivo.
    /// </summary>
    [HttpGet("measurements/latest")]
    [ProducesResponseType(typeof(MeasurementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementResponse>> GetLatest(
        [FromQuery] string? deviceId,
        CancellationToken cancellationToken)
    {
        var measurement = await _measurementService.GetLatestAsync(deviceId, cancellationToken);

        if (measurement is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Sin mediciones",
                detail: "Todavía no hay mediciones registradas.");
        }

        return Ok(measurement);
    }

    /// <summary>
    /// Lista las mediciones de un dispositivo, las más recientes primero.
    /// </summary>
    [HttpGet("devices/{deviceId}/measurements")]
    [ProducesResponseType(typeof(IReadOnlyList<MeasurementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MeasurementResponse>>> GetByDevice(
        string deviceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var items = await _measurementService.GetAsync(deviceId, from, to, limit, cancellationToken);
        return Ok(items);
    }
}
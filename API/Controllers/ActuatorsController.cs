using InvernApp.Api.Dtos;
using InvernApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Controllers;

[ApiController]
[Route("api/v1/devices/{deviceId}/actuators")]
public class ActuatorsController : ControllerBase
{
    private readonly IActuatorService _actuatorService;

    public ActuatorsController(IActuatorService actuatorService)
    {
        _actuatorService = actuatorService;
    }

    /// <summary>
    /// Estado actual de la bomba, el ventilador y la iluminación de un dispositivo
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ActuatorStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ActuatorStateResponse>>> Get(
        string deviceId,
        CancellationToken cancellationToken)
    {
        var states = await _actuatorService.GetByDeviceAsync(deviceId, cancellationToken);
        return Ok(states);
    }
}
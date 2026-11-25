using InvernApp.Api.Dtos;
using InvernApp.Api.Models;
using InvernApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Controllers;

[ApiController]
[Route("api/v1/devices/{deviceId}/commands")]
public class CommandsController : ControllerBase
{
    private readonly ICommandService _commandService;

    public CommandsController(ICommandService commandService)
    {
        _commandService = commandService;
    }

    /// <summary>
    /// Comandos pendientes (vigentes) para el ESP32, del más antiguo al más nuevo.
    /// Con ?status= devuelve el historial de ese estado (máximo 100, los más recientes primero).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CommandResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommandResponse>>> Get(
        string deviceId,
        [FromQuery] CommandStatus? status,
        CancellationToken cancellationToken)
    {
        var commands = await _commandService.GetAsync(deviceId, status, cancellationToken);
        return Ok(commands);
    }

    /// <summary>
    /// El ESP32 informa si ejecutó (executed: true) o rechazó (executed: false) un comando.
    /// Es idempotente: repetir la misma confirmación devuelve 200 sin cambios.
    /// </summary>
    [HttpPost("{commandId:long}/acknowledge")]
    [ProducesResponseType(typeof(CommandResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommandResponse>> Acknowledge(
        string deviceId,
        long commandId,
        AcknowledgeCommandRequest request,
        CancellationToken cancellationToken)
    {
        var command = await _commandService.AcknowledgeAsync(deviceId, commandId, request, cancellationToken);
        return Ok(command);
    }
}
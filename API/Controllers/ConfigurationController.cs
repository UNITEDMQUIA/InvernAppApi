using InvernApp.Api.Dtos;
using InvernApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Controllers;

[ApiController]
[Route("api/v1/configuration")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public ConfigurationController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Devuelve la configuración de automatización vigente.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConfigurationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationResponse>> Get(CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.GetAsync(cancellationToken);
        return Ok(configuration);
    }

    /// <summary>
    /// Reemplaza la configuración completa. Todos los campos son obligatorios.
    /// Si no hay cambios respecto a la configuración vigente, responde 200 sin modificar nada.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfigurationResponse>> Update(
        ConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var configuration = await _configurationService.UpdateAsync(request, cancellationToken);
        return Ok(configuration);
    }
}
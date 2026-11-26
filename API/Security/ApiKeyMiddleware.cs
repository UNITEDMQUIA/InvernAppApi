using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Security;

/// <summary>
/// Exige la cabecera X-API-Key en:
///  - toda petición que no sea de lectura (POST, PUT, DELETE...) a /api;
///  - GET /api/v1/devices/{deviceId}/commands (solo lo consulta el ESP32).
/// Las demás lecturas, /health, Swagger y el dashboard son públicos.
/// </summary>
public class ApiKeyMiddleware
{
    public const string HeaderName = "X-API-Key";

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyMiddleware> logger,
        IProblemDetailsService problemDetailsService)
    {
        _next = next;
        _logger = logger;
        _problemDetailsService = problemDetailsService;

        _expectedKey = Encoding.UTF8.GetBytes(configuration["Security:ApiKey"]!);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresKey(context.Request) || IsValid(context.Request))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning(
            "Acceso denegado (API Key ausente o inválida): {Method} {Path} desde {RemoteIp}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No autorizado",
            Detail = $"Falta la cabecera {HeaderName} o la clave es inválida."
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }

    private bool IsValid(HttpRequest request)
    {
        var provided = request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), _expectedKey);
    }

    private static bool RequiresKey(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        var isRead = HttpMethods.IsGet(request.Method)
                     || HttpMethods.IsHead(request.Method)
                     || HttpMethods.IsOptions(request.Method);

        if (!isRead)
        {
            return true;
        }

        var path = request.Path.Value?.TrimEnd('/') ?? string.Empty;
        return path.EndsWith("/commands", StringComparison.OrdinalIgnoreCase);
    }
}
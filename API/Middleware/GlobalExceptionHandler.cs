using InvernApp.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvernApp.Api.Middleware;

/// <summary>
/// Captura las excepciones no resueltas por los controllers y devuelve una respuesta uniforme (ProblemDetails):
/// BadRequestException → 400, NotFoundException → 404, ConflictException → 409;
/// cualquier otra → 500 sin exponer detalles internos.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            BadRequestException bad => (
                StatusCodes.Status400BadRequest,
                "Solicitud inválida",
                bad.Message),

            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                notFound.Message),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflicto",
                conflict.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Ocurrió un error inesperado. Consulte los registros del servidor.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Excepción no controlada en {Method} {Path} (TraceId: {TraceId})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "{Title} en {Method} {Path}: {Detail}",
                title,
                httpContext.Request.Method,
                httpContext.Request.Path,
                detail);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
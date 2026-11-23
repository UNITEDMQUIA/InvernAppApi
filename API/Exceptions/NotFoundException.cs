namespace InvernApp.Api.Exceptions;

/// <summary>
/// Se lanza cuando un recurso referenciado no existe (por ejemplo, un dispositivo no registrado).
/// El GlobalExceptionHandler la convierte en una respuesta HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
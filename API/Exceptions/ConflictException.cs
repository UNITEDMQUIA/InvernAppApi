namespace InvernApp.Api.Exceptions;

/// <summary>
/// Se lanza cuando una operación choca con el estado actual (por ejemplo, un registro duplicado).
/// El GlobalExceptionHandler la convierte en una respuesta HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {

    }
}
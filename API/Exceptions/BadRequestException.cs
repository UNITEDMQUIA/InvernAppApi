namespace InvernApp.Api.Exceptions;

/// <summary>
/// Se lanza cuando una solicitud es inválida por reglas que no se pueden expresar
/// con anotaciones (por ejemplo, un timestamp fuera de la ventana permitida).
/// El GlobalExceptionHandler la convierte en una respuesta HTTP 400.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {

    }
}

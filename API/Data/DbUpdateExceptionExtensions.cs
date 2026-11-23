using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InvernApp.Api.Data;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// True si SQL Server rechazó el guardado por un índice o restricción único
    /// (errores 2601 y 2627).
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627);
}
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ChefMetrics_BackEnd.Utilities;

/// <summary>
/// Distingue una violación real de un índice único de otros errores de
/// persistencia. Así un trigger, una restricción CHECK o una FK no se
/// presentan al usuario como "ya existe" cuando el nombre sí es nuevo.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    // 2601: fila duplicada en un índice único.
    // 2627: violación de una restricción UNIQUE.
    private static readonly int[] ErroresIndiceUnico = { 2601, 2627 };

    public static bool EsViolacionDeIndiceUnico(
        this DbUpdateException excepcion, string nombreIndice)
    {
        if (string.IsNullOrWhiteSpace(nombreIndice))
        {
            return false;
        }

        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException sql &&
                ErroresIndiceUnico.Contains(sql.Number) &&
                sql.Message.Contains(nombreIndice, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Algunos entornos de prueba no exponen SqlException. En ese caso se
        // usa el mensaje de la cadena de excepciones como respaldo.
        return excepcion.ToString()
            .Contains(nombreIndice, StringComparison.OrdinalIgnoreCase);
    }
}

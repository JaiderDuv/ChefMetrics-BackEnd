namespace ChefMetrics_BackEnd.Tests;

// Prueba de arquitectura: el código de aplicación solo debe usar EF Core.
// Las migraciones quedan fuera porque usan migrationBuilder.Sql() de forma
// controlada para crear constraints y triggers.
public sealed class SeguridadSqlTests
{
    private static readonly string[] Carpetas =
    [
        "Controllers",
        "Services",
        "Repositories",
        "Utilities",
        "Model"
    ];

    private static readonly string[] PatronesProhibidos =
    [
        "FromSqlRaw(",
        "ExecuteSqlRaw(",
        "FromSqlInterpolated(",
        "ExecuteSqlInterpolated(",
        "new SqlCommand",
        "SqlDataAdapter",
        "SqlDataReader",
        "\"SELECT ",
        "\"INSERT ",
        "\"UPDATE ",
        "\"DELETE ",
        "\"EXEC ",
        "\"DROP ",
        "\"ALTER "
    ];

    [Fact]
    public void El_codigo_de_aplicacion_no_construye_sql_crudo()
    {
        var raiz = BuscarRaizDelProyecto();
        var hallazgos = new List<string>();

        foreach (var carpeta in Carpetas)
        {
            var ruta = Path.Combine(raiz, carpeta);
            if (!Directory.Exists(ruta))
            {
                continue;
            }

            foreach (var archivo in Directory.EnumerateFiles(ruta, "*.cs", SearchOption.AllDirectories))
            {
                var lineas = File.ReadAllLines(archivo);
                for (var i = 0; i < lineas.Length; i++)
                {
                    var linea = lineas[i].Trim();
                    if (linea.StartsWith("//", StringComparison.Ordinal) ||
                        linea.StartsWith("/*", StringComparison.Ordinal) ||
                        linea.StartsWith("*", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    foreach (var patron in PatronesProhibidos)
                    {
                        if (linea.Contains(patron, StringComparison.OrdinalIgnoreCase))
                        {
                            hallazgos.Add(
                                $"{Path.GetRelativePath(raiz, archivo)}:{i + 1}: {linea}");
                        }
                    }
                }
            }
        }

        Assert.True(
            hallazgos.Count == 0,
            "Se encontró SQL crudo en el código de aplicación. " +
            "Use EF Core o justifique y aísle el acceso en un repositorio seguro:" +
            Environment.NewLine + string.Join(Environment.NewLine, hallazgos));
    }

    private static string BuscarRaizDelProyecto()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "ChefMetrics-BackEnd.csproj")))
            {
                return directorio.FullName;
            }

            directorio = directorio.Parent;
        }

        throw new DirectoryNotFoundException(
            "No se encontró ChefMetrics-BackEnd.csproj desde " + AppContext.BaseDirectory);
    }
}

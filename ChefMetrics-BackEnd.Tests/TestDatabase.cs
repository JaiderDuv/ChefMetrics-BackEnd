using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;

namespace ChefMetrics_BackEnd.Tests;

// Base aislada por caso. SQLite permite probar transacciones y operaciones en lote.
// Las migraciones y el SQL específico de SQL Server se verifican por separado.
internal sealed class TestDatabase : IDisposable
{
    public ChefMetricsContext Context { get; }
    public Cuenta Account { get; }
    public Negocio Business { get; }
    public Insumo Ingredient { get; }
    private readonly SqliteConnection? _connection;

    public TestDatabase(bool relational = false)
    {
        var options = new DbContextOptionsBuilder<ChefMetricsContext>();
        if (relational)
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _connection.CreateFunction("sysutcdatetime", () => DateTime.UtcNow);
            options.UseSqlite(_connection);
        }
        else
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }
        Context = new ChefMetricsContext(options.Options);
        if (relational)
        {
            // SQLite admite el esquema de prueba salvo el tipo SQL Server nvarchar(max).
            using var schema = _connection!.CreateCommand();
            schema.CommandText = Context.Database.GenerateCreateScript()
                .Replace("nvarchar(max)", "TEXT", StringComparison.OrdinalIgnoreCase);
            schema.ExecuteNonQuery();
        }
        Account = new Cuenta { Id = 7, Correo = "ana@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4), Activo = true, EmailVerificado = true, TipoCuenta = "emprendedor" };
        var user = new UsuarioEmprendedor { Id = 3, Cuenta = Account, Nombres = "Ana", Apellidos = "Perez", TipoFormalizacion = "no_formalizado", TipoDocumento = "CC", NumeroDocumento = "1234567", NombreEmprendimiento = "Cocina", OnboardingCompletado = true };
        Business = new Negocio { Id = 2, Usuario = user, TelefonoContacto = "3001234567", CorreoContacto = "ana@example.com", TipoEmprendimiento = new TipoEmprendimiento { Id = 1, Nombre = "Restaurante" }, PreparacionesBaseMes = 100 };
        Ingredient = new Insumo { Id = 10, Negocio = Business, Nombre = "Harina", UnidadMedida = "g", Categoria = "materia_prima", CostoUnitario = 100, Cantidad = 100, Activo = true };
        Context.Add(Ingredient);
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection?.Dispose();
    }
}

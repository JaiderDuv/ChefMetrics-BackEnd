using ChefMetrics_BackEnd.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ChefMetrics_BackEnd.Tests;

// Doble de almacenamiento aislado. Las transacciones y el SQL del proveedor
// requieren pruebas de integración con SQL Server independientes de esta suite.
internal sealed class TestDatabase : IDisposable
{
    public ChefMetricsContext Context { get; }
    public Cuenta Account { get; }
    public Negocio Business { get; }
    public Insumo Ingredient { get; }

    public TestDatabase()
    {
        Context = new ChefMetricsContext(new DbContextOptionsBuilder<ChefMetricsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        Account = new Cuenta { Id = 7, Correo = "ana@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 4), Activo = true, EmailVerificado = true, TipoCuenta = "emprendedor" };
        var user = new UsuarioEmprendedor { Id = 3, Cuenta = Account, Nombres = "Ana", Apellidos = "Perez", TipoFormalizacion = "no_formalizado", TipoDocumento = "CC", NumeroDocumento = "1234567", NombreEmprendimiento = "Cocina", OnboardingCompletado = true };
        Business = new Negocio { Id = 2, Usuario = user, TelefonoContacto = "3001234567", CorreoContacto = "ana@example.com", TipoEmprendimiento = new TipoEmprendimiento { Id = 1, Nombre = "Restaurante" }, PreparacionesBaseMes = 100 };
        Ingredient = new Insumo { Id = 10, Negocio = Business, Nombre = "Harina", UnidadMedida = "g", Categoria = "materia_prima", CostoUnitario = 100, Cantidad = 100, Activo = true };
        Context.Add(Ingredient);
        Context.SaveChanges();
    }

    public void Dispose() => Context.Dispose();
}

using Microsoft.AspNetCore.DataProtection;

namespace ChefMetrics_BackEnd.Services;

public interface ICredencialProtector
{
    string Proteger(string valor);
    string Desproteger(string valor);
}

public sealed class CredencialProtector : ICredencialProtector
{
    private readonly IDataProtector _protector;

    public CredencialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ChefMetrics.Empleado.Invitacion.v1");
    }

    public string Proteger(string valor) => _protector.Protect(valor);

    public string Desproteger(string valor) => _protector.Unprotect(valor);
}

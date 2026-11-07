namespace ChefMetrics_BackEnd.Utilities;

/// <summary>
/// Tenant data loaded while validating an employee access token for the current request.
/// </summary>
public sealed record EmpleadoRequestContext(int CuentaId, int NegocioId)
{
    public const string HttpContextItemKey = "ChefMetrics.EmpleadoRequestContext";
}

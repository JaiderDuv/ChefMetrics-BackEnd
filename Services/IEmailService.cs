namespace ChefMetrics_BackEnd.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoVerificacionAsync(string email, string token);
        Task EnviarCorreoVerificacionAsync(string destinatario, string nombreCompleto, string token);

        Task EnviarCorreoRecuperacionAsync(string email, string token);
        Task EnviarCorreoRecuperacionAsync(string destinatario, string nombreCompleto, string token);
    }
}
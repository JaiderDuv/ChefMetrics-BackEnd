namespace ChefMetrics_BackEnd.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoVerificacionAsync(string destinatario, string token);
    }
}

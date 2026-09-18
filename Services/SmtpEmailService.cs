using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ChefMetrics_BackEnd.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public SmtpEmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task EnviarCorreoVerificacionAsync(string destinatario, string token)
        {
            await EnviarCorreoAsync(
                destinatario,
                token,
                _settings.VerificationUrl,
                "Verifica tu correo electrónico - ChefMetrics",
                "verificación");
        }

        public async Task EnviarCorreoRecuperacionAsync(string destinatario, string token)
        {
            await EnviarCorreoAsync(
                destinatario,
                token,
                _settings.PasswordResetUrl,
                "Restablece tu contraseña - ChefMetrics",
                "recuperación de contraseña");
        }

        private async Task EnviarCorreoAsync(
            string destinatario,
            string token,
            string baseUrl,
            string subject,
            string emailType)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.Password) ||
                string.IsNullOrWhiteSpace(_settings.From) ||
                string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    $"La configuración SMTP del correo de {emailType} está incompleta.");
            }

            var link = QueryHelpers.AddQueryString(baseUrl, "token", token);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = $"Haz clic en el siguiente enlace: {link}",
                IsBodyHtml = false
            };
            message.To.Add(destinatario);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}
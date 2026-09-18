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
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.Password) ||
                string.IsNullOrWhiteSpace(_settings.From) ||
                string.IsNullOrWhiteSpace(_settings.VerificationUrl))
            {
                throw new InvalidOperationException(
                    "La configuración SMTP del correo de verificación está incompleta.");
            }

            var verificationUrl = QueryHelpers.AddQueryString(
                _settings.VerificationUrl, "token", token);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = "Verifica tu correo electrónico - ChefMetrics",
                Body = $"Haz clic en el siguiente enlace para verificar tu correo electrónico: {verificationUrl}",
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

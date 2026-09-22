using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ChefMetrics_BackEnd.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly IHostEnvironment _environment;

        public SmtpEmailService(
            IOptions<EmailSettings> options,
            IHostEnvironment environment)
        {
            _settings = options.Value;
            _environment = environment;
        }

        public async Task EnviarCorreoVerificacionAsync(string destinatario, string token)
        {
            await EnviarCorreoAsync(
                destinatario,
                token,
                _settings.VerificationUrl,
                "Verifica tu correo electrónico - ChefMetrics",
                ConstruirCorreoVerificacion);
        }

        public async Task EnviarCorreoRecuperacionAsync(string destinatario, string token)
        {
            await EnviarCorreoAsync(
                destinatario,
                token,
                _settings.PasswordResetUrl,
                "Restablece tu contraseña - ChefMetrics",
                ConstruirCorreoRecuperacion);
        }

        private async Task EnviarCorreoAsync(
            string destinatario,
            string token,
            string baseUrl,
            string subject,
            Func<string, string, string> construirCuerpo)
        {
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "Falta configurar la contraseña de aplicación de Gmail en Email:Password.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.From) ||
                string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException(
                    "La configuración SMTP del correo está incompleta.");
            }

            var link = QueryHelpers.AddQueryString(baseUrl, "token", token);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = construirCuerpo(destinatario, link),
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(destinatario);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            await client.SendMailAsync(message);
        }

        private string ConstruirCorreoVerificacion(string _, string link)
        {
            var template = CargarPlantilla("EmailConfirmacion_VPrevia.razor");
            var linkHtml = WebUtility.HtmlEncode(link);

            return template
                .Replace("href=\"login?confirmado=true\"", $"href=\"{linkHtml}\"", StringComparison.Ordinal)
                .Replace("href=\"login\"", $"href=\"{linkHtml}\"", StringComparison.Ordinal)
                .Replace("https://chefmetrics.com/confirmar-cuenta?token=xyz123456", linkHtml, StringComparison.Ordinal);
        }

        private string ConstruirCorreoRecuperacion(string destinatario, string link)
        {
            var template = CargarPlantilla("EmailContraseña_VPrevia.razor");
            var linkConCorreo = QueryHelpers.AddQueryString(link, "correoElectronico", destinatario);
            var linkHtml = WebUtility.HtmlEncode(linkConCorreo);

            return template
                .Replace("href=\"nueva-contraseña\"", $"href=\"{linkHtml}\"", StringComparison.Ordinal)
                .Replace("https://chefmetrics.com/nueva-contraseña?token=reset_xyz123", linkHtml, StringComparison.Ordinal);
        }

        private string CargarPlantilla(string nombreArchivo)
        {
            var rutasPosibles = new[]
            {
                Path.Combine(_environment.ContentRootPath, "EmailTemplates", nombreArchivo),
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", nombreArchivo)
            };

            var ruta = rutasPosibles.FirstOrDefault(File.Exists);
            if (ruta == null)
            {
                throw new FileNotFoundException(
                    $"No se encontró la plantilla de correo '{nombreArchivo}'.",
                    rutasPosibles[0]);
            }

            var lineas = File.ReadAllText(ruta, Encoding.UTF8)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Where(linea => !linea.TrimStart().StartsWith("@page ", StringComparison.Ordinal));

            return string.Join(Environment.NewLine, lineas);
        }
    }
}

using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
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

        public async Task EnviarCorreoVerificacionAsync(string email, string token)
        {
            await EnviarCorreoVerificacionAsync(email, "Usuario", token);
        }

        public async Task EnviarCorreoVerificacionAsync(string destinatario, string nombreCompleto, string token)
        {
            await EnviarCorreoAsync(
                destinatario,
                token,
                _settings.VerificationUrl,
                "Verifica tu correo electrónico - ChefMetrics",
                (email, link) => ConstruirCorreoVerificacion(email, nombreCompleto, link));
        }

        public async Task EnviarCorreoRecuperacionAsync(string email, string token)
        {
            await EnviarCorreoRecuperacionAsync(email, "Usuario", token);
        }
        public async Task EnviarCorreoRecuperacionAsync(string destinatario, string nombreCompleto, string token)
        {
            // Puedes usar una propiedad específica como _settings.PasswordResetUrl o VerificationUrl
            string baseUrl = !string.IsNullOrWhiteSpace(_settings.PasswordResetUrl)
                ? _settings.PasswordResetUrl
                : _settings.VerificationUrl;

            await EnviarCorreoAsync(
                destinatario,
                token,
                baseUrl,
                "Restablece tu contraseña - ChefMetrics",
                (email, link) => ConstruirCorreoRecuperacion(email, nombreCompleto, link),
                // La página /nueva-contrasena exige token + correoElectronico en la URL.
                new Dictionary<string, string> { ["correoElectronico"] = destinatario });
        }

        private async Task EnviarCorreoAsync(
            string destinatario,
            string token,
            string baseUrl,
            string subject,
            Func<string, string, string> construirCuerpo,
            Dictionary<string, string>? queryExtra = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException("Falta configurar la contraseña de aplicación de Gmail en Email:Password.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.Username) ||
                string.IsNullOrWhiteSpace(_settings.From) ||
                string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("La configuración SMTP del correo está incompleta.");
            }

            var link = QueryHelpers.AddQueryString(baseUrl, "token", token);
            if (queryExtra is not null)
            {
                foreach (var (clave, valor) in queryExtra)
                {
                    link = QueryHelpers.AddQueryString(link, clave, valor);
                }
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From, "CHEF METRICS"),
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
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                Timeout = 30000
            };

            await client.SendMailAsync(message);
        }

        private string ConstruirCorreoVerificacion(string destinatario, string nombreCompleto, string link)
        {
            var linkHtml = WebUtility.HtmlEncode(link);
            var nombreHtml = WebUtility.HtmlEncode(nombreCompleto);

            return $@"
<div style=""background-color: #F3F4F6; padding: 40px 10px; font-family: 'Segoe UI', Arial, sans-serif; min-height: 100vh;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);"">
        
        <!-- Header Superior Oscuro -->
        <tr>
            <td style=""background-color: #1F2937; padding: 25px 25px 20px 25px;"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <!-- Columna 1: LOGO A LA IZQUIERDA -->
                        <td width=""60"" align=""left"" style=""vertical-align: middle; padding-right: 15px;"">
                            <img src=""https://res.cloudinary.com/h9nkn3lo/image/upload/f_auto,q_auto/Logo.png"" 
                                 alt=""Logo"" 
                                 width=""50"" 
                                 height=""50"" 
                                 style=""display: block; border: 0; max-width: 50px; height: auto;"" />
                        </td>

                        <!-- Columna 2: TÍTULO Y SUBTÍTULO A LA DERECHA DEL LOGO -->
                        <td align=""left"" style=""vertical-align: middle;"">
                            <h1 style=""color: #F59E0B; margin: 0; font-size: 22px; font-weight: bold; letter-spacing: 1px; line-height: 1.2;"">
                                CHEF <span style=""color: #ffffff;"">METRICS</span>
                            </h1>
                            <p style=""color: #D1D5DB; font-size: 11px; margin: 4px 0 0 0; text-transform: uppercase; letter-spacing: 1.5px;"">
                                Red de Emprendimiento Gastronómico
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        <tr>
            <td align=""center"" style=""background-color: #374151; padding: 12px 20px;"">
                <p style=""color: #F3F4F6; font-size: 13px; font-weight: 600; letter-spacing: 2px; margin: 0; text-transform: uppercase;"">
                    BIENVENIDO A NUESTRA COMUNIDAD
                </p>
            </td>
        </tr>
        <tr>
            <td align=""center"" style=""padding: 40px 30px 20px 30px;"">
                <div style=""width: 64px; height: 64px; background-color: #D97706; border-radius: 50%; display: inline-block; text-align: center; line-height: 64px; margin-bottom: 20px; box-shadow: 0 4px 10px rgba(217, 119, 6, 0.3);"">
                    <span style=""color: #ffffff; font-size: 28px; vertical-align: middle;"">&#x2709;</span>
                </div>
                <h2 style=""color: #111827; font-size: 24px; margin-top: 0; margin-bottom: 12px; font-weight: 700;"">
                    ¡Hola, {nombreHtml}!
                </h2>
                <div style=""width: 50px; height: 3px; background-color: #F59E0B; margin: 0 auto 24px auto; border-radius: 2px;""></div>
                <p style=""color: #4B5563; font-size: 15px; line-height: 1.6; margin-bottom: 30px; max-width: 480px;"">
                    Nos alegra mucho tenerte con nosotros. Para completar tu registro y comenzar a hacer crecer tu emprendimiento en <strong>Chef Metrics</strong>, confirma tu cuenta haciendo clic en el siguiente botón:
                </p>
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 10px auto 30px auto;"">
                    <tr>
                        <td align=""center"" bgcolor=""#D97706"" style=""border-radius: 50px;"">
                            <a href=""{linkHtml}"" target=""_blank"" style=""display: inline-block; padding: 16px 40px; font-size: 14px; color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 50px; background-color: #D97706; letter-spacing: 1px; text-transform: uppercase;"">
                                VERIFICAR CUENTA
                            </a>
                        </td>
                    </tr>
                </table>
                <p style=""color: #9CA3AF; font-size: 12px; margin-top: 25px; margin-bottom: 0;"">
                    Si no realizaste este registro, puedes ignorar este correo.
                </p>
            </td>
        </tr>
        <tr>
            <td style=""padding: 0 30px;"">
                <hr style=""border: 0; border-top: 1px solid #FDE68A; margin: 10px 0 0 0;"" />
            </td>
        </tr>
        <tr>
            <td align=""center"" style=""background-color: #F9FAFB; padding: 25px 20px; color: #9CA3AF; font-size: 12px;"">
                <p style=""color: #D97706; font-size: 13px; font-weight: bold; margin: 0 0 6px 0; letter-spacing: 1px;"">
                    CHEF METRICS
                </p>
                <p style=""margin: 0; color: #6B7280; font-size: 12px;"">
                    Estás recibiendo este correo porque eres un miembro valioso de nuestra comunidad.
                </p>
            </td>
        </tr>
    </table>
</div>";
        }

        private string ConstruirCorreoRecuperacion(string destinatario, string nombreCompleto, string link)
        {
            var linkHtml = WebUtility.HtmlEncode(link);
            var nombreHtml = WebUtility.HtmlEncode(nombreCompleto);

            return $@"
<div style=""background-color: #F3F4F6; padding: 40px 10px; font-family: 'Segoe UI', Arial, sans-serif; min-height: 100vh;"">
    <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 600px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);"">
        
        <!-- Header Superior Oscuro -->
        <tr>
            <td style=""background-color: #1F2937; padding: 25px 25px 20px 25px;"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                    <tr>
                        <!-- Columna 1: LOGO A LA IZQUIERDA -->
                        <td width=""60"" align=""left"" style=""vertical-align: middle; padding-right: 15px;"">
                            <img src=""https://res.cloudinary.com/h9nkn3lo/image/upload/f_auto,q_auto/Logo.png"" 
                                 alt=""Logo"" 
                                 width=""50"" 
                                 height=""50"" 
                                 style=""display: block; border: 0; max-width: 50px; height: auto;"" />
                        </td>

                        <!-- Columna 2: TÍTULO Y SUBTÍTULO A LA DERECHA DEL LOGO -->
                        <td align=""left"" style=""vertical-align: middle;"">
                            <h1 style=""color: #F59E0B; margin: 0; font-size: 22px; font-weight: bold; letter-spacing: 1px; line-height: 1.2;"">
                                CHEF <span style=""color: #ffffff;"">METRICS</span>
                            </h1>
                            <p style=""color: #D1D5DB; font-size: 11px; margin: 4px 0 0 0; text-transform: uppercase; letter-spacing: 1.5px;"">
                                Red de Emprendimiento Gastronómico
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>

        <!-- Sub-banner -->
        <tr>
            <td align=""center"" style=""background-color: #374151; padding: 12px 20px;"">
                <p style=""color: #F3F4F6; font-size: 13px; font-weight: 600; letter-spacing: 2px; margin: 0; text-transform: uppercase;"">
                    RECUPERACIÓN DE ACCESO
                </p>
            </td>
        </tr>

        <!-- Cuerpo del Correo -->
        <tr>
            <td align=""center"" style=""padding: 40px 30px 20px 30px;"">
                <!-- Icono de Candado / Llave en Círculo Naranja -->
                <div style=""width: 64px; height: 64px; background-color: #D97706; border-radius: 50%; display: inline-block; text-align: center; line-height: 64px; margin-bottom: 20px; box-shadow: 0 4px 10px rgba(217, 119, 6, 0.3);"">
                    <span style=""color: #ffffff; font-size: 28px; vertical-align: middle;"">&#x1F511;</span>
                </div>

                <h2 style=""color: #111827; font-size: 24px; margin-top: 0; margin-bottom: 12px; font-weight: 700;"">
                    ¡Hola, {nombreHtml}!
                </h2>

                <div style=""width: 50px; height: 3px; background-color: #F59E0B; margin: 0 auto 24px auto; border-radius: 2px;""></div>

                <p style=""color: #4B5563; font-size: 15px; line-height: 1.6; margin-bottom: 30px; max-width: 480px;"">
                    Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>Chef Metrics</strong>. Haz clic en el siguiente botón para ingresar tu nueva contraseña:
                </p>

                <!-- Botón Redondeado -->
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 10px auto 30px auto;"">
                    <tr>
                        <td align=""center"" bgcolor=""#D97706"" style=""border-radius: 50px;"">
                            <a href=""{linkHtml}"" target=""_blank"" style=""display: inline-block; padding: 16px 40px; font-size: 14px; color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 50px; background-color: #D97706; letter-spacing: 1px; text-transform: uppercase;"">
                                RESTABLECER CONTRASEÑA
                            </a>
                        </td>
                    </tr>
                </table>

                <p style=""color: #9CA3AF; font-size: 12px; margin-top: 25px; margin-bottom: 0;"">
                    Si no solicitaste este cambio, puedes ignorar este mensaje de forma segura. Tu contraseña actual no cambiará.
                </p>
            </td>
        </tr>

        <!-- Divisor Naranja Suave -->
        <tr>
            <td style=""padding: 0 30px;"">
                <hr style=""border: 0; border-top: 1px solid #FDE68A; margin: 10px 0 0 0;"" />
            </td>
        </tr>

        <!-- Footer -->
        <tr>
            <td align=""center"" style=""background-color: #F9FAFB; padding: 25px 20px; color: #9CA3AF; font-size: 12px;"">
                <p style=""color: #D97706; font-size: 13px; font-weight: bold; margin: 0 0 6px 0; letter-spacing: 1px;"">
                    CHEF METRICS
                </p>
                <p style=""margin: 0; color: #6B7280; font-size: 12px;"">
                    Estás recibiendo este correo porque solicitaste la recuperación de tu contraseña.
                </p>
            </td>
        </tr>
    </table>
</div>";
        }
    }
}
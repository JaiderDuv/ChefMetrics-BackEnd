namespace ChefMetrics_BackEnd.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BrevoApiKey { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = string.Empty;
        public string PasswordResetUrl { get; set; } = string.Empty;
        public string InvitationUrl { get; set; } = string.Empty;

        public bool UsaBrevo => !string.IsNullOrWhiteSpace(BrevoApiKey);

        public bool PuedeEnviar => !string.IsNullOrWhiteSpace(From) &&
            (UsaBrevo || (!string.IsNullOrWhiteSpace(Host) &&
                !string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password)));
    }
}

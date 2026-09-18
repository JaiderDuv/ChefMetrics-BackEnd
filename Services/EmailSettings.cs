namespace ChefMetrics_BackEnd.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string VerificationUrl { get; set; } = string.Empty;
        public string PasswordResetUrl { get; set; } = string.Empty;
    }
}
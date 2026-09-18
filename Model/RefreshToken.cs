namespace ChefMetrics_BackEnd.Model;

public partial class RefreshToken
{
    public int IdRefreshToken { get; set; }

    public int IdUsuarioEmprendedor { get; set; }

    public string? TokenVerificacionHash { get; set; }

    public DateTime? TokenVerificacionExpira { get; set; }

    public bool CorreoVerificado { get; set; }

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpira { get; set; }

    public bool RefreshTokenRevocado { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpira { get; set; }

    public bool PasswordResetTokenUsado { get; set; }

    public virtual UsuarioEmprendedor IdUsuarioEmprendedorNavigation { get; set; } = null!;

}
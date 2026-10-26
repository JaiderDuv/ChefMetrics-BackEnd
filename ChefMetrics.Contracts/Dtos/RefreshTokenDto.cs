using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

public class RefreshTokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

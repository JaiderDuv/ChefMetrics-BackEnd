using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public class RefreshTokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

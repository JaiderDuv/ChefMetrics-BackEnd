using System.ComponentModel.DataAnnotations;

namespace ChefMetrics.Contracts;

public class TokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

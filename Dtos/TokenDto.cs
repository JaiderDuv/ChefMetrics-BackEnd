using System.ComponentModel.DataAnnotations;

namespace ChefMetrics_BackEnd.Dtos;

public class TokenDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

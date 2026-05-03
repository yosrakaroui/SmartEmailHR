using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.DTOs;

public sealed class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string MotDePasse { get; set; } = string.Empty;
}

public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpireLe { get; set; }
    public UserSummaryDto Utilisateur { get; set; } = new();
}

public sealed class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Actif { get; set; }
}


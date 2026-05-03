using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.DTOs;

public sealed class CreateUserRequestDto
{
    [Required, MaxLength(200)]
    public string Nom { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string MotDePasse { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}

public sealed class UpdateUserStatusRequestDto
{
    public bool Actif { get; set; }
}


using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.DTOs;

public sealed class GenerateEmailRequestDto
{
    [Required]
    public Guid CandidatureId { get; set; }

    [Required, MaxLength(20)]
    public string Decision { get; set; } = string.Empty;
}

public sealed class GenerateEmailResponseDto
{
    public string Sujet { get; set; } = string.Empty;
    public string Corps { get; set; } = string.Empty;
}

public sealed class EnvoyerEmailRequestDto
{
    [Required]
    public Guid CandidatureId { get; set; }

    [Required, MaxLength(20)]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? Sujet { get; set; }

    public string? Corps { get; set; }

    public bool MettreAJourStatut { get; set; } = true;
}

public sealed class EnvoyerEmailResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? HttpStatusCode { get; set; }
}


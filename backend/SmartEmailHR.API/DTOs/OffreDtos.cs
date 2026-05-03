using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.DTOs;

public sealed class CreateOffreRequestDto
{
    [Required, MaxLength(200)]
    public string Titre { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<string> CompetencesRequises { get; set; } = new();

    [Required, MaxLength(50)]
    public string NiveauExperience { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Domaine { get; set; } = string.Empty;

    [Required]
    public DateTime DateExpiration { get; set; }
}

public sealed class UpdateOffreRequestDto
{
    [MaxLength(200)]
    public string? Titre { get; set; }

    public string? Description { get; set; }

    public List<string>? CompetencesRequises { get; set; }

    [MaxLength(50)]
    public string? NiveauExperience { get; set; }

    [MaxLength(100)]
    public string? Domaine { get; set; }

    public DateTime? DateExpiration { get; set; }

    [MaxLength(20)]
    public string? Statut { get; set; }
}

public class OffreListItemDto
{
    public Guid Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> CompetencesRequises { get; set; } = new();
    public string NiveauExperience { get; set; } = string.Empty;
    public string Domaine { get; set; } = string.Empty;
    public DateTime DateExpiration { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public Guid CreePar { get; set; }
    public int NombreCandidatures { get; set; }
}

public sealed class OffreDetailDto : OffreListItemDto
{
    public List<CandidatureListItemDto> Candidatures { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace SmartEmailHR.API.DTOs;

public sealed class RecevoirCandidatureRequestDto
{
    [Required, MaxLength(260)]
    public string ObjetEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NomCandidat { get; set; }

    [Required, EmailAddress, MaxLength(320)]
    public string EmailCandidat { get; set; } = string.Empty;

    [Required]
    public string ContenuCv { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? CvUrl { get; set; }
}

public sealed class RecevoirCandidatureResponseDto
{
    public Guid CandidatureId { get; set; }
    public Guid OffreId { get; set; }
    public string Statut { get; set; } = string.Empty;
    public int Score { get; set; }
    public string DecisionSuggeree { get; set; } = string.Empty;
    public bool OffreFermee { get; set; }
}

public sealed class AnalyseIaDto
{
    public int Score { get; set; }
    public string ResumeCompetences { get; set; } = string.Empty;
    public List<string> CompetencesDetectees { get; set; } = new();
    public string Classification { get; set; } = string.Empty;
    public bool CoherencePoste { get; set; }
    public string DecisionSuggeree { get; set; } = string.Empty;
    public DateTime DateAnalyse { get; set; }
}

public class CandidatureListItemDto
{
    public Guid Id { get; set; }
    public Guid OffreId { get; set; }
    public string TitreOffre { get; set; } = string.Empty;
    public string Domaine { get; set; } = string.Empty;
    public string NomCandidat { get; set; } = string.Empty;
    public string EmailCandidat { get; set; } = string.Empty;
    public DateTime DateReception { get; set; }
    public string Statut { get; set; } = string.Empty;
    public bool EmailReponseEnvoye { get; set; }
    public string? CvUrl { get; set; }
    public AnalyseIaDto? AnalyseIA { get; set; }
}

public sealed class CandidatureDetailDto : CandidatureListItemDto
{
    public string ContenuCv { get; set; } = string.Empty;
    public string ObjetEmail { get; set; } = string.Empty;
}

public sealed class DecisionCandidatureRequestDto
{
    [Required, MaxLength(20)]
    public string Decision { get; set; } = string.Empty;

    public bool EnvoyerEmail { get; set; } = true;

    public string? SujetEmail { get; set; }
    public string? CorpsEmail { get; set; }
}

public sealed class DecisionCandidatureResponseDto
{
    public Guid CandidatureId { get; set; }
    public string Statut { get; set; } = string.Empty;
    public bool EmailReponseEnvoye { get; set; }
    public string SujetEmail { get; set; } = string.Empty;
    public string CorpsEmail { get; set; } = string.Empty;
}

public sealed class UpdateEmailStatusRequestDto
{
    public bool EmailReponseEnvoye { get; set; }
    public string? Erreur { get; set; }
}

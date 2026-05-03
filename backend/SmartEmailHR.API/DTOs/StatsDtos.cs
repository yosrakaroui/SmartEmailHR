namespace SmartEmailHR.API.DTOs;

public sealed class GlobalStatsResponseDto
{
    public int TotalCandidatures { get; set; }
    public int CandidaturesAcceptees { get; set; }
    public int CandidaturesRefusees { get; set; }
    public int CandidaturesEnAttente { get; set; }
    public int OffresActives { get; set; }
    public int OffresExpirees { get; set; }
    public int OffresDesactivees { get; set; }
    public List<DomainStatsDto> StatsParDomaine { get; set; } = new();
    public List<ScoreStatsDto> TopCandidats { get; set; } = new();
    public List<ScoreStatsDto> FaiblesScores { get; set; } = new();
}

public sealed class DomainStatsDto
{
    public string Domaine { get; set; } = string.Empty;
    public int NombreCandidatures { get; set; }
    public int Acceptees { get; set; }
    public int Refusees { get; set; }
}

public sealed class ScoreStatsDto
{
    public Guid CandidatureId { get; set; }
    public string NomCandidat { get; set; } = string.Empty;
    public string TitreOffre { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Statut { get; set; } = string.Empty;
}

public sealed class EmailLogDto
{
    public Guid Id { get; set; }
    public Guid CandidatureId { get; set; }
    public string NomCandidat { get; set; } = string.Empty;
    public string Destinataire { get; set; } = string.Empty;
    public string TypeDecision { get; set; } = string.Empty;
    public string Sujet { get; set; } = string.Empty;
    public bool Reussi { get; set; }
    public string? Erreur { get; set; }
    public DateTime DateEnvoi { get; set; }
}


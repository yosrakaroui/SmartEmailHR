namespace SmartEmailHR.API.Services;

public sealed class AiAnalysisResult
{
    public int Score { get; set; }
    public string ResumeCompetences { get; set; } = string.Empty;
    public List<string> CompetencesDetectees { get; set; } = new();
    public string Classification { get; set; } = "Général";
    public bool CoherencePoste { get; set; }
    public string DecisionSuggeree { get; set; } = "À examiner";
}

public sealed class GeneratedEmailResult
{
    public string Sujet { get; set; } = string.Empty;
    public string Corps { get; set; } = string.Empty;
}

public sealed class EmailDispatchRequest
{
    public Guid CandidatureId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string NomCandidat { get; set; } = string.Empty;
    public string EmailDestinataire { get; set; } = string.Empty;
    public string Poste { get; set; } = string.Empty;
    public string Sujet { get; set; } = string.Empty;
    public string CorpsEmail { get; set; } = string.Empty;
}

public sealed class EmailDispatchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? HttpStatusCode { get; set; }
}


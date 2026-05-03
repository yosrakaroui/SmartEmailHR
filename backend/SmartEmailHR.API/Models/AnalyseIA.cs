using System.ComponentModel.DataAnnotations;
using SmartEmailHR.API.Configuration;

namespace SmartEmailHR.API.Models;

public sealed class AnalyseIA
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CandidatureId { get; set; }

    public Candidature? Candidature { get; set; }

    public int Score { get; set; }

    public string ResumeCompetences { get; set; } = string.Empty;

    public string CompetencesDetectees { get; set; } = "[]";

    [MaxLength(100)]
    public string Classification { get; set; } = string.Empty;

    public bool CoherencePoste { get; set; }

    [MaxLength(20)]
    public string DecisionSuggeree { get; set; } = DecisionSuggestions.AExaminer;

    public DateTime DateAnalyse { get; set; } = DateTime.UtcNow;
}


using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Services;

public interface IAiService
{
    Task<AiAnalysisResult> AnalyzeCvAsync(Offre offre, string cvText, CancellationToken cancellationToken = default);

    Task<GeneratedEmailResult> GenerateDecisionEmailAsync(
        string decision,
        string nomCandidat,
        string titrePoste,
        string? resumeCompetences,
        CancellationToken cancellationToken = default);
}


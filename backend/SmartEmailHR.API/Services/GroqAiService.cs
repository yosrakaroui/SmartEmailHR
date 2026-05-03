using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Services;

public sealed class GroqAiService : IAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqAiService> _logger;

    public GroqAiService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> AnalyzeCvAsync(
        Offre offre,
        string cvText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return BuildFallbackAnalysis(offre, cvText);
        }

        try
        {
            var payload = new
            {
                model = _options.Model,
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                                  Tu es un assistant RH expert. Reponds uniquement en JSON valide.
                                  Champs attendus:
                                  score (0-100 entier),
                                  resume_competences (string),
                                  competences_detectees (array<string>),
                                  classification (string),
                                  coherence_poste (boolean),
                                  decision_suggeree (Accepte | A examiner | Refuse).
                                  """
                    },
                    new
                    {
                        role = "user",
                        content = BuildAnalysisPrompt(offre, cvText)
                    }
                }
            };

            var content = await SendGroqChatRequestAsync(payload, cancellationToken);
            var parsed = JsonSerializer.Deserialize<GroqAnalysisResponse>(ExtractJsonObject(content), JsonOptions);
            if (parsed is null)
            {
                return BuildFallbackAnalysis(offre, cvText);
            }

            return new AiAnalysisResult
            {
                Score = Math.Clamp(parsed.Score, 0, 100),
                ResumeCompetences = parsed.ResumeCompetences ?? "Resume non disponible.",
                CompetencesDetectees = parsed.CompetencesDetectees ?? new List<string>(),
                Classification = string.IsNullOrWhiteSpace(parsed.Classification) ? offre.Domaine : parsed.Classification,
                CoherencePoste = parsed.CoherencePoste,
                DecisionSuggeree = NormalizeDecisionSuggestion(parsed.DecisionSuggeree)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur Groq lors de l'analyse CV. Bascule en mode fallback.");
            return BuildFallbackAnalysis(offre, cvText);
        }
    }

    public async Task<GeneratedEmailResult> GenerateDecisionEmailAsync(
        string decision,
        string nomCandidat,
        string titrePoste,
        string? resumeCompetences,
        CancellationToken cancellationToken = default)
    {
        var normalizedDecision = NormalizeDecision(decision);
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return BuildFallbackEmail(normalizedDecision, nomCandidat, titrePoste, resumeCompetences);
        }

        try
        {
            var payload = new
            {
                model = _options.Model,
                temperature = 0.35,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = """
                                  Tu es assistant RH. Reponds uniquement en JSON valide.
                                  Champs attendus:
                                  sujet (string)
                                  corps (string)
                                  Le ton doit etre professionnel, courtois et personnalise.
                                  """
                    },
                    new
                    {
                        role = "user",
                        content = BuildEmailPrompt(normalizedDecision, nomCandidat, titrePoste, resumeCompetences)
                    }
                }
            };

            var content = await SendGroqChatRequestAsync(payload, cancellationToken);
            var parsed = JsonSerializer.Deserialize<GroqEmailResponse>(ExtractJsonObject(content), JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Corps))
            {
                return BuildFallbackEmail(normalizedDecision, nomCandidat, titrePoste, resumeCompetences);
            }

            var fallback = BuildFallbackEmail(normalizedDecision, nomCandidat, titrePoste, resumeCompetences);
            return new GeneratedEmailResult
            {
                Sujet = string.IsNullOrWhiteSpace(parsed.Sujet) ? fallback.Sujet : parsed.Sujet,
                Corps = parsed.Corps
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erreur Groq lors de la generation email. Bascule en mode fallback.");
            return BuildFallbackEmail(normalizedDecision, nomCandidat, titrePoste, resumeCompetences);
        }
    }

    private async Task<string> SendGroqChatRequestAsync(object payload, CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Groq non reussi: {Status} - {Body}", response.StatusCode, rawResponse);
            throw new InvalidOperationException($"Groq API error: {(int)response.StatusCode}");
        }

        return ExtractMessageContent(rawResponse);
    }

    private static string BuildAnalysisPrompt(Offre offre, string cvText)
    {
        return $"""
                Analyse le CV d'un candidat par rapport a l'offre ci-dessous.
                Offre:
                - Titre: {offre.Titre}
                - Description: {offre.Description}
                - Competences requises: {offre.CompetencesRequises}
                - Niveau d'experience: {offre.NiveauExperience}
                - Domaine: {offre.Domaine}

                CV (texte brut):
                {cvText}
                """;
    }

    private static string BuildEmailPrompt(
        string decision,
        string nomCandidat,
        string titrePoste,
        string? resumeCompetences)
    {
        return $"""
                Genere un email RH personnalise.
                - Decision: {decision}
                - Nom candidat: {nomCandidat}
                - Poste: {titrePoste}
                - Resume des competences: {resumeCompetences ?? "Non fourni"}

                Contraintes:
                - francais professionnel
                - 120 a 180 mots
                - pas de promesses juridiques
                """;
    }

    private static string ExtractMessageContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        return start >= 0 && end > start ? content[start..(end + 1)] : "{}";
    }

    private static AiAnalysisResult BuildFallbackAnalysis(Offre offre, string cvText)
    {
        var cv = cvText.ToLowerInvariant();
        var competencesRequises = SplitSkills(offre.CompetencesRequises);

        var detectees = competencesRequises
            .Where(skill => cv.Contains(skill.ToLowerInvariant()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var score = competencesRequises.Count == 0
            ? 55
            : (int)Math.Round((double)detectees.Count / competencesRequises.Count * 100, MidpointRounding.AwayFromZero);

        var coherence = score >= 45 || cv.Contains(offre.Domaine.ToLowerInvariant());
        var decision = score >= 75
            ? DecisionSuggestions.Accepte
            : score >= 50
                ? DecisionSuggestions.AExaminer
                : DecisionSuggestions.Refuse;

        return new AiAnalysisResult
        {
            Score = Math.Clamp(score, 0, 100),
            ResumeCompetences = detectees.Count > 0
                ? $"Le profil montre des competences pertinentes: {string.Join(", ", detectees)}."
                : "Le CV contient peu d'elements alignes sur les competences demandees.",
            CompetencesDetectees = detectees,
            Classification = string.IsNullOrWhiteSpace(offre.Domaine) ? "General" : offre.Domaine,
            CoherencePoste = coherence,
            DecisionSuggeree = decision
        };
    }

    private static GeneratedEmailResult BuildFallbackEmail(
        string decision,
        string nomCandidat,
        string titrePoste,
        string? resumeCompetences)
    {
        if (decision == CandidatureStatuts.Accepte)
        {
            return new GeneratedEmailResult
            {
                Sujet = $"Suite favorable - {titrePoste}",
                Corps =
                    $"Bonjour {nomCandidat},\n\n" +
                    $"Nous vous remercions pour votre candidature au poste de {titrePoste}. " +
                    "Apres etude de votre profil, nous avons le plaisir de vous informer que votre candidature est retenue " +
                    "pour la prochaine etape du processus.\n\n" +
                    $"Points forts releves: {resumeCompetences ?? "profil globalement aligne avec nos besoins"}.\n\n" +
                    "Nous reviendrons vers vous rapidement pour proposer un creneau d'entretien.\n\n" +
                    "Cordialement,\nL'equipe RH SmartEmail"
            };
        }

        return new GeneratedEmailResult
        {
            Sujet = $"Retour sur votre candidature - {titrePoste}",
            Corps =
                $"Bonjour {nomCandidat},\n\n" +
                $"Merci pour l'interet porte au poste de {titrePoste} et pour le temps consacre a votre candidature. " +
                "Apres analyse de votre dossier, nous ne donnons pas suite pour cette opportunite.\n\n" +
                $"Synthese de l'evaluation: {resumeCompetences ?? "adequation partielle avec les attentes du poste"}.\n\n" +
                "Nous vous encourageons a postuler a nouveau sur de futures offres mieux alignees avec votre profil.\n\n" +
                "Cordialement,\nL'equipe RH SmartEmail"
        };
    }

    private static List<string> SplitSkills(string rawSkills)
    {
        return rawSkills
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string NormalizeDecisionSuggestion(string? rawDecision)
    {
        if (string.IsNullOrWhiteSpace(rawDecision))
        {
            return DecisionSuggestions.AExaminer;
        }

        var normalized = rawDecision.Trim().ToLowerInvariant();
        if (normalized.Contains("accept"))
        {
            return DecisionSuggestions.Accepte;
        }

        if (normalized.Contains("refus") || normalized.Contains("reject"))
        {
            return DecisionSuggestions.Refuse;
        }

        return DecisionSuggestions.AExaminer;
    }

    private static string NormalizeDecision(string rawDecision)
    {
        var normalized = rawDecision.Trim().ToLowerInvariant();
        if (normalized.Contains("accept"))
        {
            return CandidatureStatuts.Accepte;
        }

        return CandidatureStatuts.Refuse;
    }

    private sealed class GroqAnalysisResponse
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("resume_competences")]
        public string? ResumeCompetences { get; set; }

        [JsonPropertyName("competences_detectees")]
        public List<string>? CompetencesDetectees { get; set; }

        [JsonPropertyName("classification")]
        public string? Classification { get; set; }

        [JsonPropertyName("coherence_poste")]
        public bool CoherencePoste { get; set; }

        [JsonPropertyName("decision_suggeree")]
        public string? DecisionSuggeree { get; set; }
    }

    private sealed class GroqEmailResponse
    {
        [JsonPropertyName("sujet")]
        public string? Sujet { get; set; }

        [JsonPropertyName("corps")]
        public string? Corps { get; set; }
    }
}


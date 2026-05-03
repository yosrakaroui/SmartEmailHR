using System.Text.Json;
using SmartEmailHR.API.DTOs;
using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Helpers;

public static class MappingExtensions
{
    public static List<string> ParseSkills(string rawSkills)
    {
        return rawSkills
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    public static string JoinSkills(IEnumerable<string> skills)
    {
        return string.Join(", ", skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public static List<string> ParseDetectedSkills(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return new List<string>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(rawJson);
            return list ?? new List<string>();
        }
        catch
        {
            return rawJson
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }

    public static string ToJsonArray(IEnumerable<string> skills)
    {
        return JsonSerializer.Serialize(skills);
    }

    public static UserSummaryDto ToSummaryDto(this User user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            Nom = user.Nom,
            Email = user.Email,
            Role = user.Role,
            Actif = user.Actif
        };
    }

    public static OffreListItemDto ToListDto(this Offre offre)
    {
        return new OffreListItemDto
        {
            Id = offre.Id,
            Titre = offre.Titre,
            Description = offre.Description,
            CompetencesRequises = ParseSkills(offre.CompetencesRequises),
            NiveauExperience = offre.NiveauExperience,
            Domaine = offre.Domaine,
            DateExpiration = offre.DateExpiration,
            Statut = offre.Statut,
            DateCreation = offre.DateCreation,
            CreePar = offre.CreePar,
            NombreCandidatures = offre.Candidatures.Count
        };
    }

    public static AnalyseIaDto? ToDto(this AnalyseIA? analyse)
    {
        if (analyse is null)
        {
            return null;
        }

        return new AnalyseIaDto
        {
            Score = analyse.Score,
            ResumeCompetences = analyse.ResumeCompetences,
            CompetencesDetectees = ParseDetectedSkills(analyse.CompetencesDetectees),
            Classification = analyse.Classification,
            CoherencePoste = analyse.CoherencePoste,
            DecisionSuggeree = analyse.DecisionSuggeree,
            DateAnalyse = analyse.DateAnalyse
        };
    }

    public static CandidatureListItemDto ToListDto(this Candidature candidature)
    {
        return new CandidatureListItemDto
        {
            Id = candidature.Id,
            OffreId = candidature.OffreId,
            TitreOffre = candidature.Offre?.Titre ?? string.Empty,
            Domaine = candidature.Offre?.Domaine ?? string.Empty,
            NomCandidat = candidature.NomCandidat,
            EmailCandidat = candidature.EmailCandidat,
            DateReception = candidature.DateReception,
            Statut = candidature.Statut,
            EmailReponseEnvoye = candidature.EmailReponseEnvoye,
            CvUrl = candidature.CvUrl,
            AnalyseIA = candidature.AnalyseIA.ToDto()
        };
    }

    public static CandidatureDetailDto ToDetailDto(this Candidature candidature)
    {
        return new CandidatureDetailDto
        {
            Id = candidature.Id,
            OffreId = candidature.OffreId,
            TitreOffre = candidature.Offre?.Titre ?? string.Empty,
            Domaine = candidature.Offre?.Domaine ?? string.Empty,
            NomCandidat = candidature.NomCandidat,
            EmailCandidat = candidature.EmailCandidat,
            DateReception = candidature.DateReception,
            Statut = candidature.Statut,
            EmailReponseEnvoye = candidature.EmailReponseEnvoye,
            CvUrl = candidature.CvUrl,
            AnalyseIA = candidature.AnalyseIA.ToDto(),
            ContenuCv = candidature.ContenuCV,
            ObjetEmail = candidature.ObjetEmail
        };
    }
}


using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Data;

public sealed class DataSeeder
{
    private readonly AppDbContext _dbContext;

    public DataSeeder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var adminId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var rhId = Guid.Parse("10000000-0000-0000-0000-000000000002");

        var admin = new User
        {
            Id = adminId,
            Nom = "Admin SmartEmail",
            Email = "admin@smartemailhr.local",
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12),
            Role = Roles.Admin,
            DateCreation = DateTime.UtcNow,
            Actif = true
        };

        var rh = new User
        {
            Id = rhId,
            Nom = "Responsable RH Demo",
            Email = "rh@smartemailhr.local",
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("Rh@123456", 12),
            Role = Roles.Rh,
            DateCreation = DateTime.UtcNow,
            Actif = true
        };

        var offreFrontend = new Offre
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Titre = "Développeur Frontend Angular",
            Description = "Concevoir des interfaces Angular performantes et maintenables.",
            CompetencesRequises = "Angular, TypeScript, HTML, CSS, RxJS",
            NiveauExperience = "Junior",
            Domaine = "Développement Web",
            DateExpiration = DateTime.UtcNow.AddDays(30),
            Statut = OffreStatuts.Active,
            DateCreation = DateTime.UtcNow.AddDays(-2),
            CreePar = rhId
        };

        var offreData = new Offre
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Titre = "Data Analyst",
            Description = "Analyser les données RH et proposer des indicateurs décisionnels.",
            CompetencesRequises = "SQL, Python, PowerBI, Statistiques",
            NiveauExperience = "Confirmé",
            Domaine = "Data",
            DateExpiration = DateTime.UtcNow.AddDays(18),
            Statut = OffreStatuts.Active,
            DateCreation = DateTime.UtcNow.AddDays(-4),
            CreePar = rhId
        };

        var offreExpiree = new Offre
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
            Titre = "Ingénieur Réseau",
            Description = "Gestion de la sécurité, du monitoring et de l'infrastructure réseau.",
            CompetencesRequises = "Cisco, Firewall, TCP/IP, Monitoring",
            NiveauExperience = "Senior",
            Domaine = "Réseaux",
            DateExpiration = DateTime.UtcNow.AddDays(-3),
            Statut = OffreStatuts.Expiree,
            DateCreation = DateTime.UtcNow.AddDays(-45),
            CreePar = rhId
        };

        var candidatureAcceptee = new Candidature
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            OffreId = offreFrontend.Id,
            NomCandidat = "Lina Boukhris",
            EmailCandidat = "lina.boukhris@example.com",
            ContenuCV = "Développeuse Angular avec 2 ans d'expérience en TypeScript, RxJS et UI design.",
            ObjetEmail = "Candidature - Développeur Frontend Angular",
            DateReception = DateTime.UtcNow.AddDays(-1),
            Statut = CandidatureStatuts.Accepte,
            EmailReponseEnvoye = true,
            CvUrl = "https://example.local/cv/lina-boukhris.pdf"
        };

        var analyseAcceptee = new AnalyseIA
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            CandidatureId = candidatureAcceptee.Id,
            Score = 91,
            ResumeCompetences = "Profil solide en Angular, TypeScript et architecture de composants réutilisables.",
            CompetencesDetectees = JsonSerializer.Serialize(new[] { "Angular", "TypeScript", "RxJS", "HTML", "CSS" }),
            Classification = "Développement Web",
            CoherencePoste = true,
            DecisionSuggeree = DecisionSuggestions.Accepte,
            DateAnalyse = DateTime.UtcNow.AddDays(-1)
        };

        var candidatureRefusee = new Candidature
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            OffreId = offreData.Id,
            NomCandidat = "Karim El Idrissi",
            EmailCandidat = "karim.elidrissi@example.com",
            ContenuCV = "Profil orienté support IT, peu d'expérience SQL et data visualisation.",
            ObjetEmail = "Candidature Data Analyst",
            DateReception = DateTime.UtcNow.AddHours(-36),
            Statut = CandidatureStatuts.Refuse,
            EmailReponseEnvoye = true,
            CvUrl = "https://example.local/cv/karim-elidrissi.pdf"
        };

        var analyseRefusee = new AnalyseIA
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
            CandidatureId = candidatureRefusee.Id,
            Score = 32,
            ResumeCompetences = "Compétences majoritairement non alignées avec l'offre Data Analyst.",
            CompetencesDetectees = JsonSerializer.Serialize(new[] { "Support IT", "Helpdesk" }),
            Classification = "Support",
            CoherencePoste = false,
            DecisionSuggeree = DecisionSuggestions.Refuse,
            DateAnalyse = DateTime.UtcNow.AddHours(-35)
        };

        var candidatureEnAttente = new Candidature
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
            OffreId = offreFrontend.Id,
            NomCandidat = "Ines Meriem",
            EmailCandidat = "ines.meriem@example.com",
            ContenuCV = "Stagiaire Angular, connaissances HTML/CSS, notions TypeScript.",
            ObjetEmail = "Application Angular Frontend Position",
            DateReception = DateTime.UtcNow.AddHours(-10),
            Statut = CandidatureStatuts.EnAttente,
            EmailReponseEnvoye = false,
            CvUrl = "https://example.local/cv/ines-meriem.pdf"
        };

        var analyseEnAttente = new AnalyseIA
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
            CandidatureId = candidatureEnAttente.Id,
            Score = 67,
            ResumeCompetences = "Bon potentiel frontend, manque d'expérience sur RxJS et architecture Angular avancée.",
            CompetencesDetectees = JsonSerializer.Serialize(new[] { "Angular", "TypeScript", "HTML", "CSS" }),
            Classification = "Développement Web",
            CoherencePoste = true,
            DecisionSuggeree = DecisionSuggestions.AExaminer,
            DateAnalyse = DateTime.UtcNow.AddHours(-9)
        };

        var logs = new List<EmailLog>
        {
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
                CandidatureId = candidatureAcceptee.Id,
                TypeDecision = EmailDecisionTypes.Acceptation,
                Sujet = "Votre candidature a été retenue - Développeur Frontend Angular",
                Corps = "Bonjour Lina, nous avons le plaisir de vous inviter à un entretien.",
                Destinataire = candidatureAcceptee.EmailCandidat,
                Reussi = true,
                DateEnvoi = DateTime.UtcNow.AddHours(-18)
            },
            new()
            {
                Id = Guid.Parse("50000000-0000-0000-0000-000000000002"),
                CandidatureId = candidatureRefusee.Id,
                TypeDecision = EmailDecisionTypes.Refus,
                Sujet = "Retour concernant votre candidature - Data Analyst",
                Corps = "Bonjour Karim, merci pour votre intérêt. Votre candidature n'a pas été retenue.",
                Destinataire = candidatureRefusee.EmailCandidat,
                Reussi = true,
                DateEnvoi = DateTime.UtcNow.AddHours(-20)
            }
        };

        await _dbContext.Users.AddRangeAsync(new[] { admin, rh }, cancellationToken);
        await _dbContext.Offres.AddRangeAsync(new[] { offreFrontend, offreData, offreExpiree }, cancellationToken);
        await _dbContext.Candidatures.AddRangeAsync(
            new[] { candidatureAcceptee, candidatureRefusee, candidatureEnAttente },
            cancellationToken);
        await _dbContext.AnalysesIA.AddRangeAsync(new[] { analyseAcceptee, analyseRefusee, analyseEnAttente }, cancellationToken);
        await _dbContext.EmailLogs.AddRangeAsync(logs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


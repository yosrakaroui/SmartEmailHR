using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.DTOs;
using SmartEmailHR.API.Helpers;
using SmartEmailHR.API.Models;
using SmartEmailHR.API.Services;

namespace SmartEmailHR.API.Controllers;

[ApiController]
[Route("api/candidatures")]
public sealed class CandidaturesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IOfferLifecycleService _lifecycleService;
    private readonly IAiService _aiService;
    private readonly IEmailWorkflowService _emailWorkflowService;
    private readonly IN8nSecretValidator _n8nSecretValidator;

    public CandidaturesController(
        AppDbContext dbContext,
        IOfferLifecycleService lifecycleService,
        IAiService aiService,
        IEmailWorkflowService emailWorkflowService,
        IN8nSecretValidator n8nSecretValidator)
    {
        _dbContext = dbContext;
        _lifecycleService = lifecycleService;
        _aiService = aiService;
        _emailWorkflowService = emailWorkflowService;
        _n8nSecretValidator = n8nSecretValidator;
    }

    [HttpPost("recevoir")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RecevoirCandidatureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecevoirCandidatureResponseDto>> Receive(
        [FromBody] RecevoirCandidatureRequestDto request,
        CancellationToken cancellationToken)
    {
        var providedSecret = Request.Headers["X-N8N-Secret"].FirstOrDefault();
        if (!_n8nSecretValidator.IsValid(providedSecret))
        {
            return Unauthorized(new { message = "Requête n8n non autorisée." });
        }

        await _lifecycleService.UpdateExpiredOffersAsync(cancellationToken);

        var matchedOffer = await FindMatchingOfferAsync(request.ObjetEmail, cancellationToken);
        if (matchedOffer is null)
        {
            return BadRequest(new { message = "Aucune offre correspondante trouvée pour l'objet de l'email." });
        }

        var now = DateTime.UtcNow;
        var isOfferClosed = matchedOffer.Statut != OffreStatuts.Active || matchedOffer.DateExpiration.Date < now.Date;

        var candidature = new Candidature
        {
            OffreId = matchedOffer.Id,
            NomCandidat = BuildCandidateName(request.NomCandidat, request.EmailCandidat),
            EmailCandidat = request.EmailCandidat.Trim().ToLowerInvariant(),
            ContenuCV = request.ContenuCv.Trim(),
            ObjetEmail = request.ObjetEmail.Trim(),
            DateReception = now,
            CvUrl = request.CvUrl,
            Statut = isOfferClosed ? CandidatureStatuts.Refuse : CandidatureStatuts.EnAttente
        };

        AiAnalysisResult analysis;
        if (isOfferClosed)
        {
            analysis = new AiAnalysisResult
            {
                Score = 0,
                ResumeCompetences = "Offre fermée: candidature refusée automatiquement.",
                CompetencesDetectees = new List<string>(),
                Classification = matchedOffer.Domaine,
                CoherencePoste = false,
                DecisionSuggeree = DecisionSuggestions.Refuse
            };
        }
        else
        {
            analysis = await _aiService.AnalyzeCvAsync(matchedOffer, request.ContenuCv, cancellationToken);
        }

        var analyseEntity = new AnalyseIA
        {
            CandidatureId = candidature.Id,
            Score = Math.Clamp(analysis.Score, 0, 100),
            ResumeCompetences = analysis.ResumeCompetences,
            CompetencesDetectees = MappingExtensions.ToJsonArray(analysis.CompetencesDetectees),
            Classification = analysis.Classification,
            CoherencePoste = analysis.CoherencePoste,
            DecisionSuggeree = analysis.DecisionSuggeree,
            DateAnalyse = now
        };

        candidature.AnalyseIA = analyseEntity;
        await _dbContext.Candidatures.AddAsync(candidature, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (isOfferClosed)
        {
            var generatedEmail = await _aiService.GenerateDecisionEmailAsync(
                CandidatureStatuts.Refuse,
                candidature.NomCandidat,
                matchedOffer.Titre,
                analysis.ResumeCompetences,
                cancellationToken);

            var dispatch = await _emailWorkflowService.SendDecisionEmailAsync(
                new EmailDispatchRequest
                {
                    CandidatureId = candidature.Id,
                    Decision = CandidatureStatuts.Refuse,
                    NomCandidat = candidature.NomCandidat,
                    EmailDestinataire = candidature.EmailCandidat,
                    Poste = matchedOffer.Titre,
                    Sujet = generatedEmail.Sujet,
                    CorpsEmail = generatedEmail.Corps
                },
                cancellationToken);

            candidature.EmailReponseEnvoye = dispatch.Success;
            await _dbContext.EmailLogs.AddAsync(
                BuildEmailLog(
                    candidature.Id,
                    CandidatureStatuts.Refuse,
                    generatedEmail.Sujet,
                    generatedEmail.Corps,
                    candidature.EmailCandidat,
                    dispatch),
                cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new RecevoirCandidatureResponseDto
        {
            CandidatureId = candidature.Id,
            OffreId = matchedOffer.Id,
            Statut = candidature.Statut,
            Score = analyseEntity.Score,
            DecisionSuggeree = analyseEntity.DecisionSuggeree,
            OffreFermee = isOfferClosed
        });
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<CandidatureListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CandidatureListItemDto>>> GetAll(
        [FromQuery] Guid? offreId,
        [FromQuery] string? statut,
        [FromQuery] string? domaine,
        [FromQuery] string? recherche,
        CancellationToken cancellationToken)
    {
        await _lifecycleService.UpdateExpiredOffersAsync(cancellationToken);

        var query = _dbContext.Candidatures
            .AsNoTracking()
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .AsQueryable();

        if (offreId.HasValue)
        {
            query = query.Where(c => c.OffreId == offreId.Value);
        }

        if (!string.IsNullOrWhiteSpace(statut))
        {
            var normalizedStatus = NormalizeDecision(statut);
            query = query.Where(c => c.Statut == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(domaine))
        {
            var normalizedDomain = domaine.Trim().ToLowerInvariant();
            query = query.Where(c => c.Offre != null && c.Offre.Domaine.ToLower() == normalizedDomain);
        }

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var q = recherche.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.NomCandidat.ToLower().Contains(q) ||
                c.EmailCandidat.ToLower().Contains(q) ||
                (c.AnalyseIA != null && c.AnalyseIA.ResumeCompetences.ToLower().Contains(q)));
        }

        var candidatures = await query
            .OrderByDescending(c => c.AnalyseIA != null ? c.AnalyseIA.Score : 0)
            .ThenByDescending(c => c.DateReception)
            .ToListAsync(cancellationToken);

        return Ok(candidatures.Select(c => c.ToListDto()).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CandidatureDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidatureDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var candidature = await _dbContext.Candidatures
            .AsNoTracking()
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (candidature is null)
        {
            return NotFound(new { message = "Candidature introuvable." });
        }

        return Ok(candidature.ToDetailDto());
    }

    [HttpPatch("{id:guid}/decision")]
    [Authorize(Policy = AuthorizationPolicies.RhOrAdmin)]
    [ProducesResponseType(typeof(DecisionCandidatureResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DecisionCandidatureResponseDto>> UpdateDecision(
        Guid id,
        [FromBody] DecisionCandidatureRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedDecision = NormalizeDecision(request.Decision);
        if (normalizedDecision != CandidatureStatuts.Accepte && normalizedDecision != CandidatureStatuts.Refuse)
        {
            return BadRequest(new { message = "Décision invalide. Valeurs attendues: Accepte ou Refuse." });
        }

        var candidature = await _dbContext.Candidatures
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (candidature is null || candidature.Offre is null)
        {
            return NotFound(new { message = "Candidature introuvable." });
        }

        candidature.Statut = normalizedDecision;

        var generatedEmail = new GeneratedEmailResult
        {
            Sujet = request.SujetEmail ?? string.Empty,
            Corps = request.CorpsEmail ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(generatedEmail.Sujet) || string.IsNullOrWhiteSpace(generatedEmail.Corps))
        {
            generatedEmail = await _aiService.GenerateDecisionEmailAsync(
                normalizedDecision,
                candidature.NomCandidat,
                candidature.Offre.Titre,
                candidature.AnalyseIA?.ResumeCompetences,
                cancellationToken);
        }

        EmailDispatchResult? dispatch = null;
        if (request.EnvoyerEmail)
        {
            dispatch = await _emailWorkflowService.SendDecisionEmailAsync(
                new EmailDispatchRequest
                {
                    CandidatureId = candidature.Id,
                    Decision = normalizedDecision,
                    NomCandidat = candidature.NomCandidat,
                    EmailDestinataire = candidature.EmailCandidat,
                    Poste = candidature.Offre.Titre,
                    Sujet = generatedEmail.Sujet,
                    CorpsEmail = generatedEmail.Corps
                },
                cancellationToken);

            candidature.EmailReponseEnvoye = dispatch.Success;

            await _dbContext.EmailLogs.AddAsync(
                BuildEmailLog(
                    candidature.Id,
                    normalizedDecision,
                    generatedEmail.Sujet,
                    generatedEmail.Corps,
                    candidature.EmailCandidat,
                    dispatch),
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new DecisionCandidatureResponseDto
        {
            CandidatureId = candidature.Id,
            Statut = candidature.Statut,
            EmailReponseEnvoye = candidature.EmailReponseEnvoye,
            SujetEmail = generatedEmail.Sujet,
            CorpsEmail = generatedEmail.Corps
        });
    }

    [HttpPatch("{id:guid}/email-envoye")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmailStatus(
        Guid id,
        [FromBody] UpdateEmailStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var providedSecret = Request.Headers["X-N8N-Secret"].FirstOrDefault();
        if (!_n8nSecretValidator.IsValid(providedSecret))
        {
            return Unauthorized(new { message = "Requête n8n non autorisée." });
        }

        var candidature = await _dbContext.Candidatures.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (candidature is null)
        {
            return NotFound(new { message = "Candidature introuvable." });
        }

        candidature.EmailReponseEnvoye = request.EmailReponseEnvoye;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Statut email mis à jour." });
    }

    private async Task<Offre?> FindMatchingOfferAsync(string objetEmail, CancellationToken cancellationToken)
    {
        var normalizedSubject = objetEmail.Trim().ToLowerInvariant();
        var offres = await _dbContext.Offres
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ranked = offres
            .Select(offre => new
            {
                Offre = offre,
                Score = CalculateMatchScore(normalizedSubject, offre.Titre)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Offre.DateCreation)
            .FirstOrDefault();

        return ranked?.Offre;
    }

    private static int CalculateMatchScore(string normalizedSubject, string titreOffre)
    {
        var normalizedTitle = titreOffre.Trim().ToLowerInvariant();
        if (normalizedSubject.Contains(normalizedTitle))
        {
            return 100;
        }

        var titleWords = normalizedTitle
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        var score = titleWords.Count(word => normalizedSubject.Contains(word)) * 10;
        return Math.Min(score, 95);
    }

    private static string BuildCandidateName(string? providedName, string email)
    {
        if (!string.IsNullOrWhiteSpace(providedName))
        {
            return providedName.Trim();
        }

        var prefix = email.Split('@', StringSplitOptions.RemoveEmptyEntries)[0];
        var cleaned = prefix.Replace(".", " ").Replace("_", " ").Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "Candidat Inconnu";
        }

        return string.Join(' ',
            cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(chunk => char.ToUpperInvariant(chunk[0]) + chunk[1..]));
    }

    private static string NormalizeDecision(string decision)
    {
        var normalized = decision.Trim().ToLowerInvariant();
        if (normalized.Contains("accept"))
        {
            return CandidatureStatuts.Accepte;
        }

        if (normalized.Contains("refus"))
        {
            return CandidatureStatuts.Refuse;
        }

        if (normalized.Contains("attente"))
        {
            return CandidatureStatuts.EnAttente;
        }

        return CandidatureStatuts.EnAttente;
    }

    private static EmailLog BuildEmailLog(
        Guid candidatureId,
        string decision,
        string sujet,
        string corps,
        string destinataire,
        EmailDispatchResult dispatch)
    {
        return new EmailLog
        {
            CandidatureId = candidatureId,
            TypeDecision = decision == CandidatureStatuts.Accepte
                ? EmailDecisionTypes.Acceptation
                : EmailDecisionTypes.Refus,
            Sujet = sujet,
            Corps = corps,
            Destinataire = destinataire,
            Reussi = dispatch.Success,
            Erreur = dispatch.Success ? null : dispatch.Error,
            DateEnvoi = DateTime.UtcNow
        };
    }
}

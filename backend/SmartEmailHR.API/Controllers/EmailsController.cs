using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.DTOs;
using SmartEmailHR.API.Models;
using SmartEmailHR.API.Services;

namespace SmartEmailHR.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RhOrAdmin)]
[Route("api/emails")]
public sealed class EmailsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAiService _aiService;
    private readonly IEmailWorkflowService _emailWorkflowService;

    public EmailsController(
        AppDbContext dbContext,
        IAiService aiService,
        IEmailWorkflowService emailWorkflowService)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _emailWorkflowService = emailWorkflowService;
    }

    [HttpPost("generer")]
    [ProducesResponseType(typeof(GenerateEmailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GenerateEmailResponseDto>> Generate(
        [FromBody] GenerateEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var candidature = await _dbContext.Candidatures
            .AsNoTracking()
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .FirstOrDefaultAsync(c => c.Id == request.CandidatureId, cancellationToken);

        if (candidature is null || candidature.Offre is null)
        {
            return NotFound(new { message = "Candidature introuvable." });
        }

        var normalizedDecision = NormalizeDecision(request.Decision);
        var generated = await _aiService.GenerateDecisionEmailAsync(
            normalizedDecision,
            candidature.NomCandidat,
            candidature.Offre.Titre,
            candidature.AnalyseIA?.ResumeCompetences,
            cancellationToken);

        return Ok(new GenerateEmailResponseDto
        {
            Sujet = generated.Sujet,
            Corps = generated.Corps
        });
    }

    [HttpPost("envoyer")]
    [ProducesResponseType(typeof(EnvoyerEmailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnvoyerEmailResponseDto>> Send(
        [FromBody] EnvoyerEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var candidature = await _dbContext.Candidatures
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .FirstOrDefaultAsync(c => c.Id == request.CandidatureId, cancellationToken);

        if (candidature is null || candidature.Offre is null)
        {
            return NotFound(new { message = "Candidature introuvable." });
        }

        var normalizedDecision = NormalizeDecision(request.Decision);
        if (normalizedDecision != CandidatureStatuts.Accepte && normalizedDecision != CandidatureStatuts.Refuse)
        {
            return BadRequest(new { message = "Décision invalide." });
        }

        GeneratedEmailResult generated;
        if (string.IsNullOrWhiteSpace(request.Sujet) || string.IsNullOrWhiteSpace(request.Corps))
        {
            generated = await _aiService.GenerateDecisionEmailAsync(
                normalizedDecision,
                candidature.NomCandidat,
                candidature.Offre.Titre,
                candidature.AnalyseIA?.ResumeCompetences,
                cancellationToken);
        }
        else
        {
            generated = new GeneratedEmailResult
            {
                Sujet = request.Sujet,
                Corps = request.Corps
            };
        }

        var dispatch = await _emailWorkflowService.SendDecisionEmailAsync(
            new EmailDispatchRequest
            {
                CandidatureId = candidature.Id,
                Decision = normalizedDecision,
                NomCandidat = candidature.NomCandidat,
                EmailDestinataire = candidature.EmailCandidat,
                Poste = candidature.Offre.Titre,
                Sujet = generated.Sujet,
                CorpsEmail = generated.Corps
            },
            cancellationToken);

        candidature.EmailReponseEnvoye = dispatch.Success;
        if (request.MettreAJourStatut)
        {
            candidature.Statut = normalizedDecision;
        }

        await _dbContext.EmailLogs.AddAsync(new EmailLog
        {
            CandidatureId = candidature.Id,
            TypeDecision = normalizedDecision == CandidatureStatuts.Accepte
                ? EmailDecisionTypes.Acceptation
                : EmailDecisionTypes.Refus,
            Sujet = generated.Sujet,
            Corps = generated.Corps,
            Destinataire = candidature.EmailCandidat,
            Reussi = dispatch.Success,
            Erreur = dispatch.Error,
            DateEnvoi = DateTime.UtcNow
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new EnvoyerEmailResponseDto
        {
            Success = dispatch.Success,
            Error = dispatch.Error,
            HttpStatusCode = dispatch.HttpStatusCode
        });
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

        return CandidatureStatuts.Refuse;
    }
}

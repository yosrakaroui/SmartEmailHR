using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.DTOs;

namespace SmartEmailHR.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/stats")]
public sealed class StatsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public StatsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("global")]
    [ProducesResponseType(typeof(GlobalStatsResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GlobalStatsResponseDto>> GetGlobal(CancellationToken cancellationToken)
    {
        var total = await _dbContext.Candidatures.CountAsync(cancellationToken);
        var acceptees = await _dbContext.Candidatures.CountAsync(c => c.Statut == CandidatureStatuts.Accepte, cancellationToken);
        var refusees = await _dbContext.Candidatures.CountAsync(c => c.Statut == CandidatureStatuts.Refuse, cancellationToken);
        var enAttente = await _dbContext.Candidatures.CountAsync(c => c.Statut == CandidatureStatuts.EnAttente, cancellationToken);

        var offresActives = await _dbContext.Offres.CountAsync(o => o.Statut == OffreStatuts.Active, cancellationToken);
        var offresExpirees = await _dbContext.Offres.CountAsync(o => o.Statut == OffreStatuts.Expiree, cancellationToken);
        var offresDesactivees = await _dbContext.Offres.CountAsync(o => o.Statut == OffreStatuts.Desactivee, cancellationToken);

        var byDomain = await _dbContext.Candidatures
            .Include(c => c.Offre)
            .GroupBy(c => c.Offre != null ? c.Offre.Domaine : "Inconnu")
            .Select(group => new DomainStatsDto
            {
                Domaine = group.Key,
                NombreCandidatures = group.Count(),
                Acceptees = group.Count(c => c.Statut == CandidatureStatuts.Accepte),
                Refusees = group.Count(c => c.Statut == CandidatureStatuts.Refuse)
            })
            .OrderByDescending(x => x.NombreCandidatures)
            .ToListAsync(cancellationToken);

        var top = await _dbContext.Candidatures
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .Where(c => c.Statut == CandidatureStatuts.Accepte && c.AnalyseIA != null)
            .OrderByDescending(c => c.AnalyseIA!.Score)
            .Take(5)
            .Select(c => new ScoreStatsDto
            {
                CandidatureId = c.Id,
                NomCandidat = c.NomCandidat,
                TitreOffre = c.Offre != null ? c.Offre.Titre : string.Empty,
                Score = c.AnalyseIA!.Score,
                Statut = c.Statut
            })
            .ToListAsync(cancellationToken);

        var weak = await _dbContext.Candidatures
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .Where(c => c.Statut == CandidatureStatuts.Refuse && c.AnalyseIA != null)
            .OrderBy(c => c.AnalyseIA!.Score)
            .Take(5)
            .Select(c => new ScoreStatsDto
            {
                CandidatureId = c.Id,
                NomCandidat = c.NomCandidat,
                TitreOffre = c.Offre != null ? c.Offre.Titre : string.Empty,
                Score = c.AnalyseIA!.Score,
                Statut = c.Statut
            })
            .ToListAsync(cancellationToken);

        return Ok(new GlobalStatsResponseDto
        {
            TotalCandidatures = total,
            CandidaturesAcceptees = acceptees,
            CandidaturesRefusees = refusees,
            CandidaturesEnAttente = enAttente,
            OffresActives = offresActives,
            OffresExpirees = offresExpirees,
            OffresDesactivees = offresDesactivees,
            StatsParDomaine = byDomain,
            TopCandidats = top,
            FaiblesScores = weak
        });
    }

    [HttpGet("emails-recents")]
    [ProducesResponseType(typeof(List<EmailLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmailLogDto>>> GetRecentEmails(CancellationToken cancellationToken)
    {
        var logs = await _dbContext.EmailLogs
            .AsNoTracking()
            .Include(e => e.Candidature)
            .OrderByDescending(e => e.DateEnvoi)
            .Take(30)
            .Select(e => new EmailLogDto
            {
                Id = e.Id,
                CandidatureId = e.CandidatureId,
                NomCandidat = e.Candidature != null ? e.Candidature.NomCandidat : "Inconnu",
                Destinataire = e.Destinataire,
                TypeDecision = e.TypeDecision,
                Sujet = e.Sujet,
                Reussi = e.Reussi,
                Erreur = e.Erreur,
                DateEnvoi = e.DateEnvoi
            })
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    [HttpGet("top-candidats")]
    [ProducesResponseType(typeof(List<ScoreStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ScoreStatsDto>>> GetTopCandidates(CancellationToken cancellationToken)
    {
        var top = await _dbContext.Candidatures
            .AsNoTracking()
            .Include(c => c.Offre)
            .Include(c => c.AnalyseIA)
            .Where(c => c.Statut == CandidatureStatuts.Accepte && c.AnalyseIA != null)
            .OrderByDescending(c => c.AnalyseIA!.Score)
            .Take(10)
            .Select(c => new ScoreStatsDto
            {
                CandidatureId = c.Id,
                NomCandidat = c.NomCandidat,
                TitreOffre = c.Offre != null ? c.Offre.Titre : string.Empty,
                Score = c.AnalyseIA!.Score,
                Statut = c.Statut
            })
            .ToListAsync(cancellationToken);

        return Ok(top);
    }
}


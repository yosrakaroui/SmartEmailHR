using System.Security.Claims;
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
[Authorize]
[Route("api/offres")]
public sealed class OffresController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IOfferLifecycleService _lifecycleService;

    public OffresController(AppDbContext dbContext, IOfferLifecycleService lifecycleService)
    {
        _dbContext = dbContext;
        _lifecycleService = lifecycleService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<OffreListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OffreListItemDto>>> GetAll(
        [FromQuery] string? domaine,
        [FromQuery] string? statut,
        CancellationToken cancellationToken)
    {
        await _lifecycleService.UpdateExpiredOffersAsync(cancellationToken);

        var query = _dbContext.Offres
            .AsNoTracking()
            .Include(o => o.Candidatures)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(domaine))
        {
            var normalizedDomain = domaine.Trim().ToLowerInvariant();
            query = query.Where(o => o.Domaine.ToLower() == normalizedDomain);
        }

        if (!string.IsNullOrWhiteSpace(statut))
        {
            var normalizedStatus = NormalizeOffreStatus(statut);
            query = query.Where(o => o.Statut == normalizedStatus);
        }

        var offres = await query
            .OrderByDescending(o => o.DateCreation)
            .ToListAsync(cancellationToken);

        return Ok(offres.Select(o => o.ToListDto()).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OffreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OffreDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        await _lifecycleService.UpdateExpiredOffersAsync(cancellationToken);

        var offre = await _dbContext.Offres
            .AsNoTracking()
            .Include(o => o.Candidatures)
                .ThenInclude(c => c.AnalyseIA)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (offre is null)
        {
            return NotFound(new { message = "Offre introuvable." });
        }

        var detail = new OffreDetailDto
        {
            Id = offre.Id,
            Titre = offre.Titre,
            Description = offre.Description,
            CompetencesRequises = MappingExtensions.ParseSkills(offre.CompetencesRequises),
            NiveauExperience = offre.NiveauExperience,
            Domaine = offre.Domaine,
            DateExpiration = offre.DateExpiration,
            Statut = offre.Statut,
            DateCreation = offre.DateCreation,
            CreePar = offre.CreePar,
            NombreCandidatures = offre.Candidatures.Count,
            Candidatures = offre.Candidatures
                .OrderByDescending(c => c.AnalyseIA != null ? c.AnalyseIA.Score : 0)
                .Select(c => c.ToListDto())
                .ToList()
        };

        return Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RhOrAdmin)]
    [ProducesResponseType(typeof(OffreListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OffreListItemDto>> Create(
        [FromBody] CreateOffreRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var creatorId))
        {
            return Unauthorized();
        }

        if (request.DateExpiration.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "La date d'expiration doit être aujourd'hui ou dans le futur." });
        }

        var offre = new Offre
        {
            Titre = request.Titre.Trim(),
            Description = request.Description.Trim(),
            CompetencesRequises = MappingExtensions.JoinSkills(request.CompetencesRequises),
            NiveauExperience = request.NiveauExperience.Trim(),
            Domaine = request.Domaine.Trim(),
            DateExpiration = request.DateExpiration.Date,
            DateCreation = DateTime.UtcNow,
            Statut = OffreStatuts.Active,
            CreePar = creatorId
        };

        await _dbContext.Offres.AddAsync(offre, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = offre.ToListDto();
        return CreatedAtAction(nameof(GetById), new { id = offre.Id }, dto);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RhOrAdmin)]
    [ProducesResponseType(typeof(OffreListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OffreListItemDto>> Update(
        Guid id,
        [FromBody] UpdateOffreRequestDto request,
        CancellationToken cancellationToken)
    {
        var offre = await _dbContext.Offres
            .Include(o => o.Candidatures)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (offre is null)
        {
            return NotFound(new { message = "Offre introuvable." });
        }

        if (!string.IsNullOrWhiteSpace(request.Titre))
        {
            offre.Titre = request.Titre.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            offre.Description = request.Description.Trim();
        }

        if (request.CompetencesRequises is { Count: > 0 })
        {
            offre.CompetencesRequises = MappingExtensions.JoinSkills(request.CompetencesRequises);
        }

        if (!string.IsNullOrWhiteSpace(request.NiveauExperience))
        {
            offre.NiveauExperience = request.NiveauExperience.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Domaine))
        {
            offre.Domaine = request.Domaine.Trim();
        }

        if (request.DateExpiration.HasValue)
        {
            offre.DateExpiration = request.DateExpiration.Value.Date;
            if (offre.DateExpiration < DateTime.UtcNow.Date && offre.Statut == OffreStatuts.Active)
            {
                offre.Statut = OffreStatuts.Expiree;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Statut))
        {
            offre.Statut = NormalizeOffreStatus(request.Statut);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(offre.ToListDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RhOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var offre = await _dbContext.Offres.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (offre is null)
        {
            return NotFound(new { message = "Offre introuvable." });
        }

        _dbContext.Offres.Remove(offre);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string NormalizeOffreStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "active" => OffreStatuts.Active,
            "expiree" or "expirée" => OffreStatuts.Expiree,
            "desactivee" or "désactivée" or "desactive" => OffreStatuts.Desactivee,
            _ => OffreStatuts.Active
        };
    }
}


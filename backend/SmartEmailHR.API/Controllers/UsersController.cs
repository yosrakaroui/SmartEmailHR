using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.DTOs;
using SmartEmailHR.API.Helpers;
using SmartEmailHR.API.Models;

namespace SmartEmailHR.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Nom)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(u => u.ToSummaryDto()).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserSummaryDto>> Create(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "Cet email est déjà utilisé." });
        }

        var role = NormalizeRole(request.Role);
        if (role is null)
        {
            return BadRequest(new { message = "Rôle invalide. Valeurs possibles: rh ou admin." });
        }

        var user = new User
        {
            Nom = request.Nom.Trim(),
            Email = normalizedEmail,
            MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.MotDePasse, 12),
            Role = role,
            DateCreation = DateTime.UtcNow,
            Actif = true
        };

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = user.Id }, user.ToSummaryDto());
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Utilisateur introuvable." });
        }

        user.Actif = request.Actif;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(user.ToSummaryDto());
    }

    private static string? NormalizeRole(string role)
    {
        var normalized = role.Trim().ToLowerInvariant();
        return normalized switch
        {
            Roles.Rh => Roles.Rh,
            Roles.Admin => Roles.Admin,
            _ => null
        };
    }
}


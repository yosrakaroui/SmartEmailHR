using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartEmailHR.API.Configuration;
using SmartEmailHR.API.Data;
using SmartEmailHR.API.DTOs;
using SmartEmailHR.API.Helpers;
using SmartEmailHR.API.Services;

namespace SmartEmailHR.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(
        AppDbContext dbContext,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !user.Actif)
        {
            return Unauthorized(new { message = "Identifiants invalides." });
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.MotDePasse, user.MotDePasseHash);
        if (!validPassword)
        {
            return Unauthorized(new { message = "Identifiants invalides." });
        }

        var token = _tokenService.GenerateToken(user);

        var response = new LoginResponseDto
        {
            Token = token,
            ExpireLe = DateTime.UtcNow.AddHours(_jwtOptions.ExpirationHours),
            Utilisateur = user.ToSummaryDto()
        };

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user.ToSummaryDto());
    }
}


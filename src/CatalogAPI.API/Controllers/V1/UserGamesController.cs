using Asp.Versioning;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.UserGames.GetUserGame;
using CatalogAPI.Application.UseCases.UserGames.GetUserGames;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/user-games")]
public class UserGamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserGamesController> _logger;

    public UserGamesController(IMediator mediator, ILogger<UserGamesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get user's game library (all purchased games)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDto<UserGameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResultDto<UserGameDto>>> GetUserGames(
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var userId = Guid.Parse(userContext.UserId);
        var query = new GetUserGamesQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Retrieved {Count} games from user library. UserId: {UserId}", 
            result.TotalCount, userContext.UserId);

        return Ok(result);
    }

    /// <summary>
    /// Get a specific game from user's library
    /// </summary>
    [HttpGet("{gameId}")]
    [ProducesResponseType(typeof(UserGameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserGameDto>> GetUserGame(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        var userId = Guid.Parse(userContext.UserId);
        var query = new GetUserGameQuery(userId, gameId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = "Game not found in user's library" });
        }

        return Ok(result);
    }
}

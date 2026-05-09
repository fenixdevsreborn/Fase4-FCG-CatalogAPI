using Asp.Versioning;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Games.CreateGame;
using CatalogAPI.Application.UseCases.Games.DeleteGame;
using CatalogAPI.Application.UseCases.Games.GetGames;
using CatalogAPI.Application.UseCases.Games.SearchGames;
using CatalogAPI.Application.UseCases.Games.UpdateGame;
using CatalogAPI.Domain.Exceptions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IMediator mediator, ILogger<GamesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of games
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDto<GameDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResultDto<GameDto>>> GetGames(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetGamesQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Search games using fuzzy search and relevance ordering
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedResultDto<GameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResultDto<GameDto>>> SearchGames(
        [FromQuery(Name = "q")] string query = "",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query cannot be empty." });
        }

        var result = await _mediator.Send(new SearchGamesQuery(query, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new game (requires Admin authentication)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateGame(
        [FromBody] CreateGameDto createGameDto,
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Verify Admin role (dupla verificação)
        if (!userContext.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        try
        {
            var command = new CreateGameCommand(createGameDto);
            var gameId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game created successfully. GameId: {GameId}, CreatedBy: {UserId}", 
                gameId, userContext.UserId);

            return CreatedAtAction(
                nameof(GetGames),
                new { },
                new { gameId, message = "Game created successfully" });
        }
        catch (GameAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing game (requires Admin authentication)
    /// </summary>
    [HttpPut("{gameId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGame(
        Guid gameId,
        [FromBody] UpdateGameDto updateGameDto,
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Verify Admin role (dupla verificação)
        if (!userContext.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        try
        {
            var command = new UpdateGameCommand(gameId, updateGameDto);
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game updated successfully. GameId: {GameId}, UpdatedBy: {UserId}", 
                gameId, userContext.UserId);

            return Ok(new { message = "Game updated successfully" });
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a game (requires Admin authentication)
    /// </summary>
    [HttpDelete("{gameId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Verify Admin role (dupla verificação)
        if (!userContext.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        try
        {
            var command = new DeleteGameCommand(gameId);
            await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game deleted successfully. GameId: {GameId}, DeletedBy: {UserId}", 
                gameId, userContext.UserId);

            return NoContent();
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

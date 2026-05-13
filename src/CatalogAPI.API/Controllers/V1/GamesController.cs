using Asp.Versioning;
using CatalogAPI.API.Common.Extensions;
using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Games.CreateGame;
using CatalogAPI.Application.UseCases.Games.DeleteGame;
using CatalogAPI.Application.UseCases.Games.GetGames;
using CatalogAPI.Application.UseCases.Games.SearchGames;
using CatalogAPI.Application.UseCases.Games.UpdateGame;
using CatalogAPI.Domain.Exceptions;
using CatalogAPI.Domain.Interfaces;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IGameRepository _gameRepository;
    private readonly IGameMetadataStore _gameMetadataStore;
    private readonly IGameSearchMaintenanceService _searchMaintenanceService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(
        IMediator mediator,
        IGameRepository gameRepository,
        IGameMetadataStore gameMetadataStore,
        IGameSearchMaintenanceService searchMaintenanceService,
        ILogger<GamesController> logger)
    {
        _mediator = mediator;
        _gameRepository = gameRepository;
        _gameMetadataStore = gameMetadataStore;
        _searchMaintenanceService = searchMaintenanceService;
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
    /// Get a single game by id
    /// </summary>
    [HttpGet("{gameId:guid}")]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetGame(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game == null)
        {
            return NotFound(new { message = $"Game with id {gameId} was not found." });
        }

        var metadata = await _gameMetadataStore.GetAsync(gameId, cancellationToken);
        return Ok(new GameDto
        {
            Id = game.Id,
            Name = game.Name,
            Description = game.Description,
            Price = game.Price,
            Genre = game.Genre,
            ImageUrl = game.ImageUrl,
            Developer = game.Developer,
            ReleaseDate = game.ReleaseDate,
            Tags = metadata?.Tags ?? [],
            Metadata = metadata?.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// Get search index status (requires Admin authentication)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("search/status")]
    [ProducesResponseType(typeof(GameSearchStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameSearchStatusDto>> GetSearchStatus(
        CancellationToken cancellationToken = default)
    {
        var status = await _searchMaintenanceService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// Reindex all games into OpenSearch (requires Admin authentication)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("search/reindex")]
    [ProducesResponseType(typeof(GameReindexResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameReindexResultDto>> ReindexSearch(
        CancellationToken cancellationToken = default)
    {
        var result = await _searchMaintenanceService.ReindexAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get catalog metrics for admin dashboard (requires Admin authentication)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/summary")]
    [ProducesResponseType(typeof(GameCatalogSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GameCatalogSummaryDto>> GetAdminSummary(
        CancellationToken cancellationToken = default)
    {
        var result = await _searchMaintenanceService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new game (requires Admin authentication)
    /// </summary>
    [Authorize(Roles = "Admin")]
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
        try
        {
            var command = new CreateGameCommand(createGameDto);
            var gameId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game created successfully. GameId: {GameId}, CreatedBy: {UserId}", 
                gameId, User.GetUserId());

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
    [Authorize(Roles = "Admin")]
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
        try
        {
            var command = new UpdateGameCommand(gameId, updateGameDto);
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game updated successfully. GameId: {GameId}, UpdatedBy: {UserId}", 
                gameId, User.GetUserId());

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
    [Authorize(Roles = "Admin")]
    [HttpDelete("{gameId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGame(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteGameCommand(gameId);
            await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Game deleted successfully. GameId: {GameId}, DeletedBy: {UserId}", 
                gameId, User.GetUserId());

            return NoContent();
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

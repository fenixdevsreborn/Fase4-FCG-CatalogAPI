using Asp.Versioning;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Application.UseCases.Orders.CreateOrder;
using CatalogAPI.Domain.Exceptions;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order for a game
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get user from context (set by AuthenticationMiddleware)
        var userContext = HttpContext.Items["User"] as UserContextDto;
        if (userContext == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Get correlation ID from context
        var correlationId = HttpContext.Items["CorrelationId"] as Guid? ?? Guid.NewGuid();

        try
        {
            var command = new CreateOrderCommand(
                Guid.Parse(userContext.UserId),
                request.GameId,
                correlationId);

            var orderId = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Order created successfully. OrderId: {OrderId}, GameId: {GameId}, UserId: {UserId}",
                orderId, request.GameId, userContext.UserId);

            return CreatedAtAction(
                nameof(CreateOrder),
                new { orderId },
                new { orderId, message = "Order created successfully" });
        }
        catch (GameNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (GameAlreadyPurchasedException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

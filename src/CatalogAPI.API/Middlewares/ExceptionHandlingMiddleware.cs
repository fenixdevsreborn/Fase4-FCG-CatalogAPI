using System.Net;
using CatalogAPI.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        var (statusCode, title) = exception switch
        {
            GameNotFoundException => (HttpStatusCode.NotFound, "Game not found"),
            GameAlreadyPurchasedException => (HttpStatusCode.Conflict, "Game already purchased"),
            GameAlreadyExistsException => (HttpStatusCode.Conflict, "Game already exists"),
            PublishEventFailedException => (HttpStatusCode.InternalServerError, "Event publishing failed"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = correlationId }
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}

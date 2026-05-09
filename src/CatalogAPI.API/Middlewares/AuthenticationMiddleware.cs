using CatalogAPI.Infrastructure.Services;

namespace CatalogAPI.API.Middlewares;

public class AuthenticationMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<AuthenticationMiddleware> _logger;

  public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context, IAuthService authService)
  {
    // Skip authentication for health check and swagger endpoints
    var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
    if (path.Contains("/health") || path.Contains("/swagger"))
    {
      await _next(context);
      return;
    }

    // Check if route requires authentication
    var requiresAuth = false;
    var requiresAdmin = false;
    var method = context.Request.Method;

    // Orders endpoint requires authentication (any user)
    if (method == HttpMethods.Post && path.Contains("/api/v1/orders"))
    {
      requiresAuth = true;
      requiresAdmin = false;
    }
    // UserGames endpoints require authentication (any user)
    else if (path.Contains("/api/user-games"))
    {
      requiresAuth = true;
      requiresAdmin = false;
    }
    // Game CRUD endpoints (POST, PUT, DELETE) require Admin
    else if ((method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Delete)
             && path.Contains("/api/games"))
    {
      requiresAuth = true;
      requiresAdmin = true;
    }

    if (requiresAuth)
    {
      var authHeader = context.Request.Headers.Authorization.ToString();

      if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
      {
        _logger.LogWarning("Cabeçalho de autorização ausente ou inválido");
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Autorização ausente ou inválida" });
        return;
      }

      var token = authHeader["Bearer ".Length..].Trim();

      try
      {
        var userContext = await authService.ValidateTokenAsync(token, context.RequestAborted);

        if (userContext == null)
        {
          _logger.LogWarning("Token inválido ou expirado");
          context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          await context.Response.WriteAsJsonAsync(new { message = "O token é inválido ou expirou" });
          return;
        }

        // Check if Admin role is required
        if (requiresAdmin && !userContext.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
          _logger.LogWarning("Acesso negado. Função de administrador necessária.");
          context.Response.StatusCode = StatusCodes.Status403Forbidden;
          await context.Response.WriteAsJsonAsync(new { message = "Acesso negado. Somente administradores podem acessar este recurso." });
          return;
        }

        // Store user context for use in controllers
        context.Items["User"] = userContext;

        _logger.LogInformation("User authenticated: {UserId}, {Email}, {Role}",
            userContext.UserId, userContext.Email, userContext.Role);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Autenticação falhou devido a um erro interno");
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { message = "Serviço de autenticação indisponível" });
        return;
      }
    }

    await _next(context);
  }
}

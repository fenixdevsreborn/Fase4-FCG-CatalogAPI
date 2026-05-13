using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CatalogAPI.API.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("Missing or invalid user id claim.");
        }

        return userId;
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirstValue("role")
            ?? user.FindFirstValue(ClaimTypes.Role)
            ?? string.Empty;
    }
}

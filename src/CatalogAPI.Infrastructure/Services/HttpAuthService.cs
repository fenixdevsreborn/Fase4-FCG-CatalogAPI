using System.Net.Http.Headers;
using System.Text.Json;
using CatalogAPI.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Infrastructure.Services;

public interface IAuthService
{
    Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

// DTO interno para deserializar a resposta do UsersAPI
internal class UserInfoResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class HttpAuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpAuthService> _logger;
    private readonly string _baseUrl;

    public HttpAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<HttpAuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["AuthService:BaseUrl"] ?? "http://localhost:8080";
    }

    public async Task<UserContextDto?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UsersAPI returned status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var userInfo = JsonSerializer.Deserialize<UserInfoResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null)
            {
                _logger.LogWarning("Failed to deserialize user info from UsersAPI");
                return null;
            }

            // Mapear UserInfoResponse (UserId é Guid) para UserContextDto (UserId é string)
            var userContext = new UserContextDto
            {
                UserId = userInfo.UserId.ToString(),
                Name = userInfo.Name,
                Email = userInfo.Email,
                Role = userInfo.Role
            };

            return userContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with UsersAPI");
            throw;
        }
    }
}

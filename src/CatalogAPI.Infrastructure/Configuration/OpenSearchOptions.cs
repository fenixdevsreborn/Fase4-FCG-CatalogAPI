namespace CatalogAPI.Infrastructure.Configuration;

public sealed class OpenSearchOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string IndexName { get; set; } = "fcg-games";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

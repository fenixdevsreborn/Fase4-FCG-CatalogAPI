namespace CatalogAPI.Infrastructure.Configuration;

public sealed class CatalogCacheOptions
{
    public string? ConnectionString { get; set; }
    public string InstanceName { get; set; } = "fcg-catalog";
    public int DefaultTtlMinutes { get; set; } = 5;
}

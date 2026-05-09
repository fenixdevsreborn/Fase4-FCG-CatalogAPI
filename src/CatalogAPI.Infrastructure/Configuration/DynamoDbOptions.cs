namespace CatalogAPI.Infrastructure.Configuration;

public sealed class DynamoDbOptions
{
    public bool Enabled { get; set; }
    public string TableName { get; set; } = "fcg-catalog-metadata";
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
}

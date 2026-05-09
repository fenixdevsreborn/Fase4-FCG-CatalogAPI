using System.Collections.Concurrent;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace CatalogAPI.Infrastructure.Services;

public sealed class GameMetadataStore : IGameMetadataStore
{
    private static readonly ConcurrentDictionary<Guid, GameMetadataDto> FallbackStore = new();

    private readonly IAmazonDynamoDB? _dynamoDb;
    private readonly DynamoDbOptions _options;

    public GameMetadataStore(
        IOptions<DynamoDbOptions> options,
        IAmazonDynamoDB? dynamoDb = null)
    {
        _options = options.Value;
        _dynamoDb = dynamoDb;
    }

    public async Task<GameMetadataDto?> GetAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            return FallbackStore.TryGetValue(gameId, out var metadata) ? Clone(metadata) : null;
        }

        var response = await _dynamoDb!.GetItemAsync(
            new GetItemRequest
            {
                TableName = _options.TableName,
                Key = CreateKey(gameId)
            },
            cancellationToken);

        return response.Item.Count == 0 ? null : FromItem(response.Item);
    }

    public async Task<IReadOnlyDictionary<Guid, GameMetadataDto>> GetManyAsync(
        IEnumerable<Guid> gameIds,
        CancellationToken cancellationToken = default)
    {
        var ids = gameIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, GameMetadataDto>();
        }

        if (!IsEnabled)
        {
            return ids
                .Where(id => FallbackStore.ContainsKey(id))
                .ToDictionary(id => id, id => Clone(FallbackStore[id]));
        }

        var request = new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [_options.TableName] = new KeysAndAttributes
                {
                    Keys = ids.Select(CreateKey).ToList()
                }
            }
        };

        var response = await _dynamoDb!.BatchGetItemAsync(request, cancellationToken);
        if (!response.Responses.TryGetValue(_options.TableName, out var items))
        {
            return new Dictionary<Guid, GameMetadataDto>();
        }

        return items.ToDictionary(
            item => Guid.Parse(item["GameId"].S),
            FromItem);
    }

    public async Task UpsertAsync(Guid gameId, GameMetadataDto metadata, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(metadata);

        if (!IsEnabled)
        {
            FallbackStore[gameId] = normalized;
            return;
        }

        await _dynamoDb!.PutItemAsync(
            new PutItemRequest
            {
                TableName = _options.TableName,
                Item = ToItem(gameId, normalized)
            },
            cancellationToken);
    }

    public async Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            FallbackStore.TryRemove(gameId, out _);
            return;
        }

        await _dynamoDb!.DeleteItemAsync(
            new DeleteItemRequest
            {
                TableName = _options.TableName,
                Key = CreateKey(gameId)
            },
            cancellationToken);
    }

    private bool IsEnabled =>
        _options.Enabled &&
        _dynamoDb != null &&
        !string.IsNullOrWhiteSpace(_options.TableName);

    private static Dictionary<string, AttributeValue> CreateKey(Guid gameId)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["GameId"] = new AttributeValue { S = gameId.ToString() }
        };
    }

    private static Dictionary<string, AttributeValue> ToItem(Guid gameId, GameMetadataDto metadata)
    {
        return new Dictionary<string, AttributeValue>
        {
            ["GameId"] = new AttributeValue { S = gameId.ToString() },
            ["Tags"] = new AttributeValue
            {
                L = metadata.Tags.Select(tag => new AttributeValue { S = tag }).ToList()
            },
            ["Metadata"] = new AttributeValue
            {
                M = metadata.Metadata.ToDictionary(
                    pair => pair.Key,
                    pair => new AttributeValue { S = pair.Value },
                    StringComparer.OrdinalIgnoreCase)
            }
        };
    }

    private static GameMetadataDto FromItem(Dictionary<string, AttributeValue> item)
    {
        var metadata = new GameMetadataDto();

        if (item.TryGetValue("Tags", out var tags) && tags.L.Count > 0)
        {
            metadata.Tags = tags.L
                .Select(value => value.S)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()!;
        }

        if (item.TryGetValue("Metadata", out var attributes) && attributes.M.Count > 0)
        {
            metadata.Metadata = attributes.M.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.S ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        }

        return Normalize(metadata);
    }

    private static GameMetadataDto Normalize(GameMetadataDto metadata)
    {
        return new GameMetadataDto
        {
            Tags = metadata.Tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Metadata = metadata.Metadata
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    pair => pair.Key.Trim(),
                    pair => pair.Value?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
        };
    }

    private static GameMetadataDto Clone(GameMetadataDto metadata)
    {
        return new GameMetadataDto
        {
            Tags = [.. metadata.Tags],
            Metadata = new Dictionary<string, string>(metadata.Metadata, StringComparer.OrdinalIgnoreCase)
        };
    }
}

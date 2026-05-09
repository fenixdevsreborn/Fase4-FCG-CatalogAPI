using CatalogAPI.Application.Abstractions;
using CatalogAPI.Application.DTOs;
using CatalogAPI.Infrastructure.Configuration;
using CatalogAPI.Infrastructure.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;

namespace CatalogAPI.Infrastructure.Services;

public sealed class OpenSearchGameSearchService : IGameSearchService
{
    private readonly OpenSearchOptions _options;
    private readonly ILogger<OpenSearchGameSearchService> _logger;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly OpenSearchClient? _client;
    private bool _indexEnsured;

    public OpenSearchGameSearchService(
        IOptions<OpenSearchOptions> options,
        ILogger<OpenSearchGameSearchService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (IsEnabled)
        {
            var settings = new ConnectionSettings(new Uri(_options.Endpoint))
                .DefaultIndex(_options.IndexName)
                .DefaultMappingFor<GameSearchDocument>(mapping => mapping
                    .IndexName(_options.IndexName)
                    .IdProperty(document => document.Id));

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                settings = settings.BasicAuthentication(_options.Username, _options.Password);
            }

            _client = new OpenSearchClient(settings);
        }
    }

    public bool IsEnabled =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.Endpoint);

    public async Task<PaginatedResultDto<GameDto>> SearchAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _client == null)
        {
            throw new InvalidOperationException("OpenSearch is not enabled for this environment.");
        }

        await EnsureIndexExistsAsync(cancellationToken);

        var response = await _client.SearchAsync<GameSearchDocument>(
            descriptor => descriptor
                .Index(_options.IndexName)
                .From((pageNumber - 1) * pageSize)
                .Size(pageSize)
                .Query(search => search.MultiMatch(match => match
                    .Query(query)
                    .Fields(fields => fields
                        .Field(document => document.Name, 4.0)
                        .Field(document => document.Description, 2.0)
                        .Field(document => document.Genre, 2.0)
                        .Field(document => document.Developer, 1.5)
                        .Field("tags^3"))
                    .Fuzziness(Fuzziness.Auto)
                    .Operator(Operator.And)))
                .Sort(sort => sort
                    .Descending(SortSpecialField.Score)
                    .Ascending(document => document.Name)),
            cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"OpenSearch query failed: {response.ServerError?.ToString() ?? response.OriginalException?.Message}");
        }

        return new PaginatedResultDto<GameDto>
        {
            Items = response.Hits.Select(hit => hit.Source.ToDto()).ToList(),
            TotalCount = Convert.ToInt32(response.Total),
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task IndexAsync(GameDto game, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _client == null)
        {
            return;
        }

        await EnsureIndexExistsAsync(cancellationToken);
        var document = GameSearchDocument.FromDto(game);

        var response = await _client.IndexAsync(
            document,
            descriptor => descriptor.Index(_options.IndexName).Id(document.Id),
            cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"OpenSearch index failed for game {game.Id}: {response.ServerError?.ToString() ?? response.OriginalException?.Message}");
        }
    }

    public async Task DeleteAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _client == null)
        {
            return;
        }

        await EnsureIndexExistsAsync(cancellationToken);

        var response = await _client.DeleteAsync<GameSearchDocument>(
            gameId,
            descriptor => descriptor.Index(_options.IndexName),
            cancellationToken);

        if (!response.IsValid && response.Result != Result.NotFound)
        {
            throw new InvalidOperationException($"OpenSearch delete failed for game {gameId}: {response.ServerError?.ToString() ?? response.OriginalException?.Message}");
        }
    }

    private async Task EnsureIndexExistsAsync(CancellationToken cancellationToken)
    {
        if (_indexEnsured || _client == null)
        {
            return;
        }

        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (_indexEnsured)
            {
                return;
            }

            var exists = await _client.Indices.ExistsAsync(
                new IndexExistsRequest(_options.IndexName),
                cancellationToken);
            if (!exists.Exists)
            {
                var create = await _client.Indices.CreateAsync(
                    _options.IndexName,
                    descriptor => descriptor.Map<GameSearchDocument>(map => map.AutoMap()),
                    cancellationToken);

                if (!create.IsValid)
                {
                    throw new InvalidOperationException(
                        $"OpenSearch index creation failed: {create.ServerError?.ToString() ?? create.OriginalException?.Message}");
                }

                _logger.LogInformation("Created OpenSearch index {IndexName}", _options.IndexName);
            }

            _indexEnsured = true;
        }
        finally
        {
            _indexLock.Release();
        }
    }
}

using Microsoft.Extensions.Logging;

namespace CatalogAPI.Infrastructure.Data.Seeders;

/// <summary>
/// Serviço que orquestra a execução de todos os seeders registrados.
/// Executa cada seeder individualmente, permitindo que falhas em um não impeçam os outros.
/// </summary>
public class DatabaseSeederService
{
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ILogger<DatabaseSeederService> _logger;

    public DatabaseSeederService(
        IEnumerable<ISeeder> seeders,
        ILogger<DatabaseSeederService> logger)
    {
        _seeders = seeders ?? throw new ArgumentNullException(nameof(seeders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa todos os seeders registrados em sequência.
    /// </summary>
    /// <param name="context">Contexto do banco de dados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dicionário com o nome do seeder e quantidade de registros inseridos</returns>
    public async Task<Dictionary<string, int>> SeedAllAsync(
        CatalogDbContext context, 
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, int>();
        var totalInserted = 0;

        _logger.LogInformation("Iniciando execução de {Count} seeder(s)...", _seeders.Count());

        foreach (var seeder in _seeders)
        {
            try
            {
                _logger.LogInformation("Executando seeder: {SeederName}", seeder.Name);
                var insertedCount = await seeder.SeedAsync(context, cancellationToken);
                results[seeder.Name] = insertedCount;
                totalInserted += insertedCount;
                _logger.LogInformation("Seeder {SeederName} concluído. {Count} registro(s) inserido(s)", 
                    seeder.Name, insertedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar seeder {SeederName}. Continuando com os próximos seeders...", 
                    seeder.Name);
                results[seeder.Name] = -1; // -1 indica erro
            }
        }

        _logger.LogInformation("Execução de seeders concluída. Total: {Total} registro(s) inserido(s) em {Count} seeder(s)", 
            totalInserted, _seeders.Count());

        return results;
    }
}

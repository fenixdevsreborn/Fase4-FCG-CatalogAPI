using CatalogAPI.Infrastructure.Data;

namespace CatalogAPI.Infrastructure.Data.Seeders;

/// <summary>
/// Interface para seeders de banco de dados.
/// Permite adicionar novos seeders facilmente no futuro.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Executa o seed de dados no banco de dados.
    /// </summary>
    /// <param name="context">Contexto do banco de dados</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Quantidade de registros inseridos</returns>
    Task<int> SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nome do seeder para logging e identificação.
    /// </summary>
    string Name { get; }
}

using CatalogAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogAPI.Infrastructure.Data.Seeders;

/// <summary>
/// Seeder para popular a tabela de Games com dados iniciais.
/// Valida duplicidade por ID antes de inserir.
/// </summary>
public class GameSeeder : ISeeder
{
    private readonly ILogger<GameSeeder> _logger;

    public GameSeeder(ILogger<GameSeeder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "GameSeeder";

    public async Task<int> SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Iniciando seed de Games...");

        var gamesToSeed = GetGamesToSeed();
        var insertedCount = 0;

        try
        {
            // Buscar todos os IDs existentes de uma vez para otimizar
            var existingIds = await context.Games
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            // Filtrar apenas os games que não existem
            var gamesToInsert = gamesToSeed
                .Where(g => !existingIds.Contains(g.Id))
                .ToList();

            if (gamesToInsert.Count == 0)
            {
                _logger.LogInformation("Seed de Games concluído. Nenhum registro novo para inserir");
                return 0;
            }

            // Usar transação para garantir atomicidade
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var game in gamesToInsert)
                {
                    _logger.LogDebug("Game '{Name}' (ID: {Id}) será inserido", game.Name, game.Id);
                }

                await context.Games.AddRangeAsync(gamesToInsert, cancellationToken);
                insertedCount = gamesToInsert.Count;

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                _logger.LogInformation("Seed de Games concluído. {Count} registro(s) inserido(s)", insertedCount);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Erro ao executar seed de Games. Transação revertida");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal ao executar seed de Games");
            throw;
        }

        return insertedCount;
    }

    private static List<Game> GetGamesToSeed()
    {
        return new List<Game>
        {
            new Game
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "God of War Ragnarök",
                Description = "Kratos and Atreus embark on a mythic journey for answers before Ragnarök arrives.",
                Price = 59.99m,
                Genre = "Action",
                ImageUrl = "https://image.api.playstation.com/vulcan/ap/rnd/202207/1210/4xJ8XB3bi888QTLZYdl7Oi0s.png",
                Developer = "Santa Monica Studio",
                ReleaseDate = new DateTime(2022, 11, 9, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Elden Ring",
                Description = "A new fantasy action RPG. Rise, Tarnished, and be guided by grace to brandish the power of the Elden Ring.",
                Price = 59.99m,
                Genre = "RPG",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/1245620/header.jpg",
                Developer = "FromSoftware",
                ReleaseDate = new DateTime(2022, 2, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "FIFA 25",
                Description = "Feel closer to the game with EA SPORTS FC 25. Experience the most true-to-football experience ever.",
                Price = 69.99m,
                Genre = "Sports",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/2195250/header.jpg",
                Developer = "EA Sports",
                ReleaseDate = new DateTime(2024, 9, 27, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Minecraft",
                Description = "Explore infinite worlds and build everything from simple homes to grand castles.",
                Price = 26.95m,
                Genre = "Sandbox",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/1942280/header.jpg",
                Developer = "Mojang Studios",
                ReleaseDate = new DateTime(2011, 11, 18, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Cyberpunk 2077",
                Description = "An open-world, action-adventure RPG set in the megalopolis of Night City.",
                Price = 39.99m,
                Genre = "RPG",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/1091500/header.jpg",
                Developer = "CD Projekt Red",
                ReleaseDate = new DateTime(2020, 12, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Name = "The Witcher 3: Wild Hunt",
                Description = "As Geralt of Rivia, a monster hunter for hire, journey through a rich fantasy world.",
                Price = 29.99m,
                Genre = "RPG",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/292030/header.jpg",
                Developer = "CD Projekt Red",
                ReleaseDate = new DateTime(2015, 5, 19, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Name = "Grand Theft Auto VI",
                Description = "Experience the next generation of open-world gaming with unprecedented graphics and gameplay.",
                Price = 69.99m,
                Genre = "Action",
                ImageUrl = "https://media-rockstargames-com.akamaized.net/mfe6/prod/__common/img/71d4d8a6006e148ceb6da1bcbf06c3c5.jpg",
                Developer = "Rockstar Games",
                ReleaseDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Name = "Stardew Valley",
                Description = "You've inherited your grandfather's old farm plot. Armed with hand-me-down tools, you set out to begin your new life!",
                Price = 14.99m,
                Genre = "Simulation",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/413150/header.jpg",
                Developer = "ConcernedApe",
                ReleaseDate = new DateTime(2016, 2, 26, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Name = "Hades II",
                Description = "Battle beyond the Underworld using dark sorcery as you take on the Titan of Time in this sequel.",
                Price = 29.99m,
                Genre = "Roguelike",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/1145350/header.jpg",
                Developer = "Supergiant Games",
                ReleaseDate = new DateTime(2024, 5, 6, 0, 0, 0, DateTimeKind.Utc)
            },
            new Game
            {
                Id = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                Name = "Baldur's Gate 3",
                Description = "Gather your party and return to the Forgotten Realms in a tale of fellowship and betrayal.",
                Price = 59.99m,
                Genre = "RPG",
                ImageUrl = "https://cdn.cloudflare.steamstatic.com/steam/apps/1086940/header.jpg",
                Developer = "Larian Studios",
                ReleaseDate = new DateTime(2023, 8, 3, 0, 0, 0, DateTimeKind.Utc)
            }
        };
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatalogAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Genre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Developer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGames",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGames", x => new { x.UserId, x.GameId });
                    table.ForeignKey(
                        name: "FK_UserGames_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "Description", "Developer", "Genre", "ImageUrl", "Name", "Price", "ReleaseDate" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Kratos and Atreus embark on a mythic journey for answers before Ragnarök arrives.", "Santa Monica Studio", "Action", "https://image.api.playstation.com/vulcan/ap/rnd/202207/1210/4xJ8XB3bi888QTLZYdl7Oi0s.png", "God of War Ragnarök", 59.99m, new DateTime(2022, 11, 9, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "A new fantasy action RPG. Rise, Tarnished, and be guided by grace to brandish the power of the Elden Ring.", "FromSoftware", "RPG", "https://cdn.cloudflare.steamstatic.com/steam/apps/1245620/header.jpg", "Elden Ring", 59.99m, new DateTime(2022, 2, 25, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Feel closer to the game with EA SPORTS FC 25. Experience the most true-to-football experience ever.", "EA Sports", "Sports", "https://cdn.cloudflare.steamstatic.com/steam/apps/2195250/header.jpg", "FIFA 25", 69.99m, new DateTime(2024, 9, 27, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Explore infinite worlds and build everything from simple homes to grand castles.", "Mojang Studios", "Sandbox", "https://cdn.cloudflare.steamstatic.com/steam/apps/1942280/header.jpg", "Minecraft", 26.95m, new DateTime(2011, 11, 18, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "An open-world, action-adventure RPG set in the megalopolis of Night City.", "CD Projekt Red", "RPG", "https://cdn.cloudflare.steamstatic.com/steam/apps/1091500/header.jpg", "Cyberpunk 2077", 39.99m, new DateTime(2020, 12, 10, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "As Geralt of Rivia, a monster hunter for hire, journey through a rich fantasy world.", "CD Projekt Red", "RPG", "https://cdn.cloudflare.steamstatic.com/steam/apps/292030/header.jpg", "The Witcher 3: Wild Hunt", 29.99m, new DateTime(2015, 5, 19, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "Experience the next generation of open-world gaming with unprecedented graphics and gameplay.", "Rockstar Games", "Action", "https://media-rockstargames-com.akamaized.net/mfe6/prod/__common/img/71d4d8a6006e148ceb6da1bcbf06c3c5.jpg", "Grand Theft Auto VI", 69.99m, new DateTime(2025, 3, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "You've inherited your grandfather's old farm plot. Armed with hand-me-down tools, you set out to begin your new life!", "ConcernedApe", "Simulation", "https://cdn.cloudflare.steamstatic.com/steam/apps/413150/header.jpg", "Stardew Valley", 14.99m, new DateTime(2016, 2, 26, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("99999999-9999-9999-9999-999999999999"), "Battle beyond the Underworld using dark sorcery as you take on the Titan of Time in this sequel.", "Supergiant Games", "Roguelike", "https://cdn.cloudflare.steamstatic.com/steam/apps/1145350/header.jpg", "Hades II", 29.99m, new DateTime(2024, 5, 6, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Gather your party and return to the Forgotten Realms in a tale of fellowship and betrayal.", "Larian Studios", "RPG", "https://cdn.cloudflare.steamstatic.com/steam/apps/1086940/header.jpg", "Baldur's Gate 3", 59.99m, new DateTime(2023, 8, 3, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserGames_GameId",
                table: "UserGames",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGames_UserId_GameId",
                table: "UserGames",
                columns: new[] { "UserId", "GameId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "UserGames");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}

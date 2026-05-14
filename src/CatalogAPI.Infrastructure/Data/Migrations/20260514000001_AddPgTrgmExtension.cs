using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogAPI.Infrastructure.Data.Migrations
{
    public partial class AddPgTrgmExtension : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_games_name_trgm ON ""Games"" USING gin(""Name"" gin_trgm_ops);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_games_genre_trgm ON ""Games"" USING gin(""Genre"" gin_trgm_ops);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_games_developer_trgm ON ""Games"" USING gin(""Developer"" gin_trgm_ops);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_games_developer_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_games_genre_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_games_name_trgm;");

            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}

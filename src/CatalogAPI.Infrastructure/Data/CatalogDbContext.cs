using CatalogAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogAPI.Infrastructure.Data;

public class CatalogDbContext : DbContext
{
  public CatalogDbContext() { }

  public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
  {
  }

  public DbSet<Game> Games => Set<Game>();
  public DbSet<UserGame> UserGames => Set<UserGame>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderItem> OrderItems => Set<OrderItem>();
  public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    if (!optionsBuilder.IsConfigured)
    {
      var connectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDatabase") ??
        Environment.GetEnvironmentVariable("CATALOG_DB_CONNECTION_STRING");

      if (string.IsNullOrWhiteSpace(connectionString))
      {
        throw new InvalidOperationException(
          "No Catalog database connection string configured for design-time. Set ConnectionStrings__CatalogDatabase or CATALOG_DB_CONNECTION_STRING.");
      }

      optionsBuilder.UseNpgsql(connectionString);
    }
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure Game entity
    modelBuilder.Entity<Game>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.Property(e => e.Description).HasMaxLength(1000);
      entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
      entity.Property(e => e.Genre).IsRequired().HasMaxLength(100);
      entity.Property(e => e.ImageUrl).HasMaxLength(500);
      entity.Property(e => e.Developer).IsRequired().HasMaxLength(200);
    });

    // Configure UserGame entity (Many-to-Many relationship)
    modelBuilder.Entity<UserGame>(entity =>
    {
      entity.HasKey(ug => new { ug.UserId, ug.GameId });

      entity.HasOne(ug => ug.Game)
                .WithMany(g => g.UserGames)
                .HasForeignKey(ug => ug.GameId)
                .OnDelete(DeleteBehavior.Cascade);

      entity.HasIndex(ug => new { ug.UserId, ug.GameId }).IsUnique();
    });

    // Configure OutboxMessage entity
    modelBuilder.Entity<OutboxMessage>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
      entity.Property(e => e.Payload).IsRequired().HasColumnType("text");
      entity.Property(e => e.CorrelationId).IsRequired().HasMaxLength(100);
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.Property(e => e.ProcessedAt).IsRequired(false);

      entity.HasIndex(e => e.CorrelationId);
      entity.HasIndex(e => e.ProcessedAt);
    });

    // Configure Order entity
    modelBuilder.Entity<Order>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.UserId).IsRequired();
      entity.Property(e => e.Status).IsRequired();
      entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.Property(e => e.UpdatedAt).IsRequired();

      entity.HasIndex(e => e.UserId);
      entity.HasIndex(e => e.Status);
    });

    // Configure OrderItem entity
    modelBuilder.Entity<OrderItem>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.OrderId).IsRequired();
      entity.Property(e => e.GameId).IsRequired();
      entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
      entity.Property(e => e.Status).IsRequired();

      entity.HasOne(oi => oi.Order)
              .WithMany(o => o.OrderItems)
              .HasForeignKey(oi => oi.OrderId)
              .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(oi => oi.Game)
              .WithMany()
              .HasForeignKey(oi => oi.GameId)
              .OnDelete(DeleteBehavior.Restrict);

      entity.HasIndex(e => e.OrderId);
      entity.HasIndex(e => e.GameId);
    });


  }
}


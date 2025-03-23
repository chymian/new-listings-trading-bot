
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using new_listing_bot_cs.DataAccess.Entities;

namespace new_listing_bot_cs.DataAccess;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IConfiguration configuration) : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<OrderResultEntity> OrderResults { get; init; } = null!;
    public DbSet<PortfolioEntity> Portfolio { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _configuration.GetConnectionString("Database"),
            options => options.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30)));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderResultEntity>(entity =>
        {
            entity.OwnsOne(e => e.ExchangeOrderResult);
            entity.OwnsOne(e => e.ExitStrategy);
        });

        modelBuilder.Entity<PortfolioEntity>()
            .HasOne(p => p.OrderResult)
            .WithOne()
            .HasForeignKey<PortfolioEntity>(p => p.OrderResultId);
    }
}


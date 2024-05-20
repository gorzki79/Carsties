using AuctionService.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions options) 
        : base(options)
    {
    }

    public DbSet<Auction> Auctions { get; set; }
    public DbSet<Item> Items { get; set; }

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxStateEntity();
    }

   internal static AuctionDbContext CreateContext()
    {
        return new AuctionDbContext(new DbContextOptionsBuilder<AuctionDbContext>().UseNpgsql(
                new ConfigurationBuilder()
                    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"))
                    .AddEnvironmentVariables()
                    .Build()
                    .GetConnectionString("DefaultConnection"),
                opt =>
                {
                    opt.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                }
            )
            .Options);
    }
    
}
using AuctionService.Entities;
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
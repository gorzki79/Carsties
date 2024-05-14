/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Design;

namespace AuctionService.Data;

public class AuctionsDbContextFactory : IDesignTimeDbContextFactory<AuctionsDbContext>
{
    public AuctionsDbContext CreateDbContext(string[] args)
    {
        var dbContext = AuctionsDbContext.CreateContext();
        //dbContext.Database.Migrate();
        return dbContext;
    }
}*/

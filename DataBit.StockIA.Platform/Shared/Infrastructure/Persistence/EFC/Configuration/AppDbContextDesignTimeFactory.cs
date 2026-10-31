using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Lets <c>dotnet ef migrations add</c> build the model without starting the API
///     (no database connection is opened to create a migration).
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("server=localhost;port=3306;user=root;password=root;database=stockia")
            .Options;
        return new AppDbContext(options);
    }
}

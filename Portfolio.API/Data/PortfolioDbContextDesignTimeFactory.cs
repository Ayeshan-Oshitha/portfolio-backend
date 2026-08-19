using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Npgsql;

using Portfolio.API.Enums;

namespace Portfolio.API.Data;

/// <summary>
/// Used only by <c>dotnet ef</c>. The runtime wiring lives in <c>Program.cs</c>; this exists so
/// the CLI does not have to boot the Functions host to read the model.
/// </summary>
public class PortfolioDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        // Only used to build the model; `dotnet ef database update` needs a real value here.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=portfolio;Username=postgres;Password=postgres";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TechCategory>("tech_category");
        dataSourceBuilder.MapEnum<PriceType>("price_type");
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<UserStatus>("user_status");

        var options = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        return new PortfolioDbContext(options);
    }
}

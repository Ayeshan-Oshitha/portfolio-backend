using Microsoft.EntityFrameworkCore;

using Npgsql;

using Portfolio.API.Data;
using Portfolio.API.Enums;

using Testcontainers.PostgreSql;

namespace Portfolio.Tests;

/// <summary>
/// One throwaway Postgres container for the whole test run, with the real migrations applied.
/// The schema uses native enums, citext and a partial unique index, so the in-memory provider
/// would happily pass tests for behaviour that is actually broken.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // The same enum mappings Program.cs applies — without them Npgsql cannot read the
        // native enum columns back.
        var builder = new NpgsqlDataSourceBuilder(_container.GetConnectionString());
        builder.MapEnum<TechCategory>("tech_category");
        builder.MapEnum<PriceType>("price_type");
        builder.MapEnum<UserRole>("user_role");
        builder.MapEnum<UserStatus>("user_status");
        _dataSource = builder.Build();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public PortfolioDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql(_dataSource)
            .Options);

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

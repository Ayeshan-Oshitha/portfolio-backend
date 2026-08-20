using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

using Portfolio.API.Auth;
using Portfolio.API.Common;
using Portfolio.API.Data;
using Portfolio.API.Enums;
using Portfolio.API.Interfaces;
using Portfolio.API.Middleware;
using Portfolio.API.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Use Neon's pooled connection string.");

// The enums must be mapped on the data source as well as in the model.
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<TechCategory>("tech_category");
dataSourceBuilder.MapEnum<PriceType>("price_type");
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<UserStatus>("user_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Services.AddDbContextPool<PortfolioDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
    }));

// Keep IActionResult payloads on the same JSON contract as the hand-serialised public responses.
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonDefaults.Options.PropertyNamingPolicy;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonDefaults.Options.DefaultIgnoreCondition;

    foreach (var converter in JsonDefaults.Options.Converters)
    {
        options.JsonSerializerOptions.Converters.Add(converter);
    }
});

builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IPricingService, PricingService>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SuperAdminOptions>(builder.Configuration.GetSection("SuperAdmin"));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<IUserService, UserService>();

// Everything under /api/admin/* is authorised here, not per function.
builder.UseMiddleware<JwtAuthenticationMiddleware>();

var host = builder.Build();

// The one account that is never created through the API. Schema changes still belong in
// migrations run from CI — this only writes the row, and is safe to repeat on every cold start.
await using (var scope = host.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(SuperAdminSeeder));

    try
    {
        await SuperAdminSeeder.EnsureSeededAsync(
            scope.ServiceProvider.GetRequiredService<PortfolioDbContext>(),
            scope.ServiceProvider.GetRequiredService<IOptions<SuperAdminOptions>>().Value,
            logger);
    }
    catch (Exception ex)
    {
        // A transient Neon failure must not stop the host from serving the public sites.
        logger.LogError(ex, "Super admin seeding failed. The host is starting anyway.");
    }
}

host.Run();

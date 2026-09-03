using Amazon.S3;

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

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.Docs;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Interfaces;
using FrostWoodTech.API.Media;
using FrostWoodTech.API.Middleware;
using FrostWoodTech.API.Services;

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
dataSourceBuilder.MapEnum<PasswordTokenPurpose>("password_token_purpose");
dataSourceBuilder.MapEnum<AuthAttemptAction>("auth_attempt_action");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Services.AddDbContextPool<FrostWoodTechDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql =>
    {
        // Mapping the enums on the data source (above) is not enough on its own — EF Core's
        // model needs the same mapping declared here too, or it falls back to sending enum
        // columns as plain integers, which every native enum column in Postgres rejects.
        npgsql.MapEnum<TechCategory>("tech_category");
        npgsql.MapEnum<PriceType>("price_type");
        npgsql.MapEnum<UserRole>("user_role");
        npgsql.MapEnum<UserStatus>("user_status");
        npgsql.MapEnum<PasswordTokenPurpose>("password_token_purpose");
        npgsql.MapEnum<AuthAttemptAction>("auth_attempt_action");
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
builder.Services.AddScoped<IFaqService, FaqService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

builder.Services.Configure<NeonStorageOptions>(builder.Configuration.GetSection("NeonS3"));
// The S3 client is thread-safe and meant to be long-lived, so it is built once from options
// rather than re-created per request.
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<NeonStorageOptions>>().Value;

    return new AmazonS3Client(
        options.AccessKey,
        options.SecretKey,
        new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region,
        });
});
builder.Services.AddScoped<IMediaService, NeonStorageService>();
builder.Services.AddScoped<IArticleMediaResolver, ArticleMediaResolver>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SuperAdminOptions>(builder.Configuration.GetSection("SuperAdmin"));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
// Singleton so Google's discovery document and signing keys are fetched once and cached, not
// re-fetched on every sign-in.
builder.Services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

builder.Services.AddScoped<ILoginRateLimiter, LoginRateLimiter>();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// Anything but "brevo" — including missing or blank — falls back to logging, so a deployment
// with no API key says so in the log instead of failing every send at the provider.
if (string.Equals(builder.Configuration["Email:Provider"], "brevo", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IEmailService, BrevoEmailService>(client =>
    {
        client.BaseAddress = new Uri("https://api.brevo.com/v3/");
    });
}
else
{
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();
}

// /api/docs and /api/openapi.yaml, both 404 unless Docs__Enabled is set.
builder.Services.Configure<DocsOptions>(builder.Configuration.GetSection("Docs"));

// Order matters. CORS is outermost so its headers land on error responses too — a 401 or 500
// the browser cannot read is a 401 or 500 the SPA cannot report. The exception handler then
// wraps the auth middleware as well as the functions.
builder.UseMiddleware<CorsMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

// Everything under /api/admin/* is authorised here, not per function.
builder.UseMiddleware<JwtAuthenticationMiddleware>();

var host = builder.Build();

// The one account never created through the API — seeded with no password, emailed a setup
// link. Safe to repeat on every cold start; schema changes still belong in CI migrations.
await using (var scope = host.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(SuperAdminSeeder));

    try
    {
        await SuperAdminSeeder.EnsureSeededAsync(
            scope.ServiceProvider.GetRequiredService<FrostWoodTechDbContext>(),
            scope.ServiceProvider.GetRequiredService<IOptions<SuperAdminOptions>>().Value,
            scope.ServiceProvider.GetRequiredService<IEmailService>(),
            scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>().Value,
            logger);
    }
    catch (Exception ex)
    {
        // A transient Neon failure must not stop the host from serving the public sites.
        logger.LogError(ex, "Super admin seeding failed. The host is starting anyway.");
    }
}

host.Run();

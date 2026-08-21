using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Portfolio.API.Data;

namespace Portfolio.API.Functions.Health;

public class GetHealth
{
    private readonly PortfolioDbContext _db;
    private readonly ILogger<GetHealth> _logger;

    public GetHealth(PortfolioDbContext db, ILogger<GetHealth> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Readiness, not liveness: it round-trips to Neon, because a worker that cannot reach the
    /// database is no use to the three frontends even though the host is up.
    ///
    /// The one place a raw DbContext outside the service layer is the right call — there is no
    /// aggregate here, and routing it through a service would only obscure what it checks.
    /// Expect roughly a second on the first request after Neon has been idle; that is the
    /// documented cold start, not a failure.
    /// </summary>
    [Function("GetHealth")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        req.HttpContext.Response.Headers.CacheControl = "no-store";

        try
        {
            await _db.Database.ExecuteSqlRawAsync("select 1", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check could not reach the database.");

            return new ObjectResult(new { status = "unhealthy", database = "unreachable" })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        return new OkObjectResult(new { status = "healthy", database = "ok" });
    }
}

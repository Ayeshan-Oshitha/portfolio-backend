using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Portfolio.API.Functions.Articles;

public class Articles
{
    private readonly ILogger<Articles> _logger;

    public Articles(ILogger<Articles> logger)
    {
        _logger = logger;
    }

    [Function("Articles")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        _logger.LogInformation("Articles endpoint called.");

        var article = new
        {
            Id = Guid.NewGuid(),
            Heading = "Building a Modern Portfolio with React and Azure Functions",
            Description = "Learn how to build a scalable portfolio website using React, Azure Functions, Entity Framework Core, and PostgreSQL.",
            ReadTime = "6 min read",
            PublishedDate = new DateTime(2026, 7, 10),
            ImageUrl = "https://example.com/images/react-azure-functions.jpg",
        };

        return new OkObjectResult(article);
    }
}
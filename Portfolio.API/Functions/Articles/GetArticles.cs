using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Portfolio.API.Functions.Articles;

public class GetArticles
{
    private readonly ILogger<GetArticles> _logger;

    public GetArticles(ILogger<GetArticles> logger)
    {
        _logger = logger;
    }

    [Function("GetArticlesV1")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/articles")] HttpRequest req)
    {
        var articles = new List<object>
        {
            new { Id = 1, Title = "Article 1", Content = "Content of Article 1" },
            new { Id = 2, Title = "Article 2", Content = "Content of Article 2" },
            new { Id = 3, Title = "Article 3", Content = "Content of Article 3" }
        };

        return new OkObjectResult(articles);
    }
}
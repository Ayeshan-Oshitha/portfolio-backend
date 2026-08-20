using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Portfolio.API.Common;
using Portfolio.API.Interfaces;

namespace Portfolio.API.Functions.Users;

public class GetUsers
{
    private readonly IUserService _users;

    public GetUsers(IUserService users)
    {
        _users = users;
    }

    [Function("GetUsers")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "admin/users")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        HttpResponses.MarkNoStore(req);

        var search = QueryParameters.ReadString(req, "search");
        var (page, pageSize) = QueryParameters.ReadPaging(req);

        var result = await _users.GetAllAsync(search, page, pageSize, cancellationToken);

        return new OkObjectResult(result);
    }
}

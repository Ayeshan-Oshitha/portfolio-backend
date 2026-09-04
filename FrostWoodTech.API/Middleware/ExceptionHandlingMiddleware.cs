using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Middleware;

/// <summary>
/// Backstop that keeps the RFC 7807 contract honest for unexpected throws (a bare 500 otherwise
/// has no body to switch on). Registered first so it wraps the auth middleware too.
/// </summary>
public sealed class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // The caller hung up. Nothing failed and there is nobody left to answer.
            _logger.LogInformation(
                "{FunctionName} was cancelled by the caller.",
                context.FunctionDefinition.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception in {FunctionName} ({InvocationId}).",
                context.FunctionDefinition.Name,
                context.InvocationId);

            var httpContext = context.GetHttpContext();
            if (httpContext is null)
            {
                // Not an HTTP trigger — there is no problem+json to write, so let the host see it.
                throw;
            }

            if (httpContext.Response.HasStarted)
            {
                // Too late to replace the body; the log above is the only useful record.
                throw;
            }

            // Deliberately generic: an exception message can leak a connection string or row data.
            // The invocation id ties this to the log entry.
            await ProblemResults.WriteAsync(
                httpContext.Response,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "internal_error",
                $"The request could not be completed. Invocation id: {context.InvocationId}.");
        }
    }
}

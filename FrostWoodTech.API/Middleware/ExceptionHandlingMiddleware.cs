using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.API.Middleware;

/// <summary>
/// The backstop that keeps the RFC 7807 contract honest. Functions handle the failures they can
/// name — a bad body, a missing row — but an unexpected throw (a Neon timeout, a Neon Object Storage
/// call that fell over) would otherwise reach the host and come back as a bare 500 with no body,
/// which the frontends cannot switch on.
///
/// Registered first so it wraps the auth middleware as well as the functions themselves.
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

            // The detail is deliberately generic: an exception message can carry a connection
            // string or a row's contents. The invocation id is what ties this to the log entry.
            await ProblemResults.WriteAsync(
                httpContext.Response,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "internal_error",
                $"The request could not be completed. Invocation id: {context.InvocationId}.");
        }
    }
}

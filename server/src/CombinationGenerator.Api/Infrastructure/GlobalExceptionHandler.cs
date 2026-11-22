using CombinationGenerator.Application.Common;
using CombinationGenerator.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CombinationGenerator.Api.Infrastructure;

// Translates exceptions into RFC 9457 problem details.
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title, detail) = exception switch
        {
            DomainValidationException e => (StatusCodes.Status400BadRequest, "The request is invalid.", e.Message),
            SessionNotFoundException e => (StatusCodes.Status404NotFound, "Session not found.", e.Message),
            SessionCapacityExceededException e => (StatusCodes.Status503ServiceUnavailable, "Service is busy.", e.Message),
            SessionConflictException e => (StatusCodes.Status409Conflict, "Concurrent update.", e.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized.", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception (trace {TraceId})")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string traceId);
}

using System.Text.Json;
using MoePortal.Core.Exceptions;

namespace MoePortal.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
            _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteProblemDetailsAsync(context, ex, correlationId);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception ex, string correlationId)
    {
        var (status, title) = ex switch
        {
            DomainException d           => (400, d.Message),
            KeyNotFoundException        => (404, "Resource not found."),
            UnauthorizedAccessException => (403, "Forbidden."),
            OperationCanceledException  => (499, "Request cancelled."),
            _                           => (500, "An internal server error occurred.")
        };

        // Never expose internal exception details in 5xx responses
        var detail = status < 500 ? ex.Message : "See server logs for details.";

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type          = $"https://tools.ietf.org/html/rfc7807#{status}",
            title,
            status,
            detail,
            correlationId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

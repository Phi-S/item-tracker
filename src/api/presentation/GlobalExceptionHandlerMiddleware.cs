using System.Diagnostics;
using presentation.Extension;

namespace presentation;

public class GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await next.Invoke(context);
            Log(context, sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            var elapsedMilliseconds = sw.ElapsedMilliseconds;
            await WriteInternalServerErrorResponse(context, "Unknown error");
            Log(context, elapsedMilliseconds, e);
        }
    }

    private void Log(HttpContext context, long elapsedMilliseconds, Exception? e = null)
    {
        var protocol = context.Request.Protocol;
        var method = context.Request.Method;
        var path = context.Request.Path;
        var traceId = context.TraceIdentifier;
        var statusCode = context.Response.StatusCode;
        var userId = context.User.Id();

        if (e is null)
        {
            logger.LogInformation(
                "[{Protocol} {Method} {Path}] [{TraceId}] [{UserId}] responded {StatusCode} in {ElapsedMilliseconds} ms",
                protocol, method, path, traceId, userId, statusCode, elapsedMilliseconds);
        }
        else
        {
            logger.LogError(e,
                "[{Protocol} {Method} {Path}] [{TraceId}] [{UserId}] responded {StatusCode} in {ElapsedMilliseconds} ms",
                protocol, method, path, traceId, userId, statusCode, elapsedMilliseconds);
        }
    }

    private static Task WriteInternalServerErrorResponse(HttpContext context, string message)
    {
        var response = Results.Extensions.InternalServerError(message);
        return response.ExecuteAsync(context);
    }
}
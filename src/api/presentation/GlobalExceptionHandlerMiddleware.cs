using System.Diagnostics;

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
        var userIdResult = context.User.Id();
        var userId = userIdResult.IsError ? "NoUserIdFound" : userIdResult.Value;

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["HttpContext"] = context,
               }))
        {
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
    }

    private static async Task WriteInternalServerErrorResponse(HttpContext context, string message)
    {
        var response = Results.Extensions.InternalServerError(message);
        await response.ExecuteAsync(context);
    }
}
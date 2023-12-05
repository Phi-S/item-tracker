using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace shared;

public class ErrorResult(int statusCode, string message) : IResult
{
    public int StatusCode { get; } = statusCode;
    public string Message { get; } = message;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCode;
        var resultObject = new
        {
            TraceId = httpContext.TraceIdentifier,
            Message
        };

        var responseJson = JsonSerializer.Serialize(resultObject);
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(responseJson);
        await httpContext.Response.WriteAsync(responseJson);
    }
}
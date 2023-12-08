using System.Net.Mime;
using System.Text;
using System.Text.Json;
using shared.Models;

namespace presentation.Extension;

public class ErrorResult(int statusCode, string message) : IResult
{
    public int StatusCode { get; } = statusCode;
    public string Message { get; } = message;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = StatusCode;
        var resultObject = new ErrorResultModel(httpContext.TraceIdentifier, Message);

        var responseJson = JsonSerializer.Serialize(resultObject);
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(responseJson);
        await httpContext.Response.WriteAsync(responseJson);
    }
}
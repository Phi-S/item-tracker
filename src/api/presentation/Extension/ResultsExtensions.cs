namespace presentation.Extension;

internal static class ResultsExtensions
{
    public static IResult InternalServerError(this IResultExtensions resultExtensions, string message)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return new ErrorResult(StatusCodes.Status500InternalServerError, message);
    }

    public static IResult Unauthorized(this IResultExtensions resultExtensions, string message)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return new ErrorResult(StatusCodes.Status401Unauthorized, message);
    }
}
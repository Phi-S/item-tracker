using System.Security.Claims;
using ErrorOr;
using Error = ErrorOr.Error;

namespace presentation;

public static class UserExtensionMethods
{
    public static ErrorOr<string> Id(this ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(claim => claim.Type.Equals("sid"))?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.NotFound(description: "No userid found");
        }

        return userId;
    }

    public static ErrorOr<string> Username(this ClaimsPrincipal user)
    {
        var username = user.Claims.FirstOrDefault(claim => claim.Type.Equals("preferred_username"))?.Value;
        if (string.IsNullOrWhiteSpace(username))
        {
            return Error.NotFound(description: "No username found");
        }

        return username;
    }
}
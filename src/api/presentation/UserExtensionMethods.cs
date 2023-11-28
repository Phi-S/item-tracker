using System.Security.Claims;
using ErrorOr;
using Error = ErrorOr.Error;

namespace presentation;

public static class UserExtensionMethods
{
    public static ErrorOr<string> Id(this ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(claim => claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"))?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.NotFound(description: "No userid found");
        }

        return userId;
    }
}
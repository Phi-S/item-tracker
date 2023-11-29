using System.Security.Claims;

namespace presentation;

public static class UserExtensionMethods
{
    public static string? Id(this ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(claim => claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"))?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return userId;
    }
}
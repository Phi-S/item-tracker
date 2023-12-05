using System.Security.Claims;

namespace presentation.Extension;

public static class ClaimsPrincipalExtension
{
    public static string? Id(this ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(claim =>
            claim.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"))?.Value;
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
using System.Security.Claims;

namespace presentation.Authentication;

public static class ClaimsExtensionMethods
{
    public static string UserId(this IEnumerable<Claim> claims)
    {
        return claims.First(claim => claim.Type.Equals("sub")).Value;
    }
}
using System.Security.Claims;
using shared.Models.ListResponse;

namespace presentation.Authentication;

public static class ClaimsExtensionMethods
{
    public static bool IsOwnList(this ClaimsPrincipal user, ListResponse list)
    {
        if (user.Identity is null || user.Identity.IsAuthenticated == false)
        {
            return false;
        }

        var sub = user.Claims.FirstOrDefault(claim => claim.Type.Equals("sub"));
        return sub is not null && sub.Value.Equals(list.UserId);
    }
}
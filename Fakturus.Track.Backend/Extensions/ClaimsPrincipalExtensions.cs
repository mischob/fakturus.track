using System.Security.Claims;

namespace Fakturus.Track.Backend.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetObjectId(this ClaimsPrincipal principal)
    {
        // Azure AD B2C tokens use 'sub' claim for the object identifier
        // Azure AD tokens use 'oid' claim
        return principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
               principal.FindFirst("oid")?.Value ??
               principal.FindFirst("sub")?.Value ?? // B2C uses 'sub' claim
               principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               throw new InvalidOperationException("Object ID not found in claims");
    }
}
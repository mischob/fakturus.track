using System.Security.Claims;

namespace Fakturus.Track.Frontend.Services.BetaAccess;

public interface IBetaAccessService
{
    bool IsBetaModeEnabled();
    bool HasBetaAccess(ClaimsPrincipal user);
    string GetBetaClaimName();
}

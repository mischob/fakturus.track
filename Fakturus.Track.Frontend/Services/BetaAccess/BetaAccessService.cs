using System.Security.Claims;

namespace Fakturus.Track.Frontend.Services.BetaAccess;

public class BetaAccessService(IConfiguration configuration, ILogger<BetaAccessService> logger)
    : IBetaAccessService
{
    public bool IsBetaModeEnabled()
    {
        var enabled = configuration.GetValue<bool>("Fakturus:BetaMode:Enabled", false) 
                   || configuration.GetValue<bool>("BetaMode:Enabled", false);
        return enabled;
    }

    public string GetBetaClaimName()
    {
        var claimName = configuration["Fakturus:BetaMode:ClaimName"] 
                     ?? configuration["BetaMode:ClaimName"] 
                     ?? "extension_BetaSupport";
        return claimName;
    }

    public bool HasBetaAccess(ClaimsPrincipal user)
    {
        // If beta mode is disabled, everyone has access
        if (!IsBetaModeEnabled())
        {
            logger.LogDebug("Beta mode is disabled - granting access to all users");
            return true;
        }

        // If user is not authenticated, deny access
        if (user?.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("User is not authenticated - denying access");
            return false;
        }

        var claimName = GetBetaClaimName();
        var betaClaim = user.FindFirst(claimName);

        if (betaClaim == null)
        {
            logger.LogWarning("User {User} has no beta claim '{ClaimName}' - denying access",
                user.Identity.Name, claimName);
            return false;
        }

        // Parse the claim value as boolean
        var hasBetaAccess = bool.TryParse(betaClaim.Value, out var value) && value;

        if (hasBetaAccess)
        {
            logger.LogInformation("User {User} has beta access (claim '{ClaimName}' = true)",
                user.Identity.Name, claimName);
        }
        else
        {
            logger.LogWarning("User {User} has beta claim '{ClaimName}' but value is not true (value: {Value}) - denying access",
                user.Identity.Name, claimName, betaClaim.Value);
        }

        return hasBetaAccess;
    }
}

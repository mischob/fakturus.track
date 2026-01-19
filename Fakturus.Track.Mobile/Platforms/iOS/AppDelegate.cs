using Foundation;
using Microsoft.Identity.Client;
using UIKit;

namespace Fakturus.Track.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        // Handle MSAL redirect URLs
        if (AuthenticationContinuationHelper.IsBrokerResponse(url.ToString()))
        {
            AuthenticationContinuationHelper.SetBrokerContinuationEventArgs(url);
            return true;
        }

        return base.OpenUrl(application, url, options);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
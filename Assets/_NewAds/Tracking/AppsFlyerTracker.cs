using AppsFlyerSDK;
using System.Collections.Generic;

public class AppsFlyerTracker : IAdsTracker
{
    public void TrackEvent(string eventName, object data = null)
    {
        // Gửi event tuỳ ý lên AppsFlyer
        AppsFlyer.sendEvent(eventName, data as Dictionary<string, string>);
    }

    public void TrackRevenue(string adUnit, string network, double revenue, string currency, string adType)
    {
        var eventValues = new Dictionary<string, string>
        {
            { "af_revenue", revenue.ToString("F2") },
            { "af_currency", currency },
            { "ad_unit", adUnit },
            { "network", network },
            { "ad_type", adType }
        };
        AppsFlyer.sendEvent("af_ad_revenue", eventValues);
    }

    public void TrackImpression(string adUnit, string network, string adType)
    {
        var eventValues = new Dictionary<string, string>
        {
            { "ad_unit", adUnit },
            { "network", network },
            { "ad_type", adType }
        };
        AppsFlyer.sendEvent("af_ad_impression", eventValues);
    }

    public void TrackClick(string adUnit, string network, string adType)
    {
        var eventValues = new Dictionary<string, string>
        {
            { "ad_unit", adUnit },
            { "network", network },
            { "ad_type", adType }
        };
        AppsFlyer.sendEvent("af_ad_click", eventValues);
    }
}
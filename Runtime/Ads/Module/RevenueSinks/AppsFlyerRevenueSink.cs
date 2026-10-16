#if USE_APPSFLYER
using System.Collections.Generic;
using UnityEngine;

public class AppsFlyerRevenueSink : IAdRevenueSink
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register() => AdRevenueTracker.Register(new AppsFlyerRevenueSink());

    public void Send(AdRevenueData data)
    {
        Debug.Log("TrackerAppsFlyer".Ads("AppsflyerRevenue: " + Time.time + " " + data.Revenue));
        var additionalParams = new Dictionary<string, string>
        {
            { AppsFlyerSDK.AdRevenueScheme.COUNTRY, data.CountryCode },
            { AppsFlyerSDK.AdRevenueScheme.AD_UNIT, data.AdUnitId },
            { AppsFlyerSDK.AdRevenueScheme.AD_TYPE, data.AdFormat },
            { AppsFlyerSDK.AdRevenueScheme.PLACEMENT, data.Placement }
        };
        var logRevenue = new AppsFlyerSDK.AFAdRevenueData(data.NetworkName, AppsFlyerSDK.MediationNetwork.ApplovinMax, "USD", data.Revenue);
        AppsFlyerSDK.AppsFlyer.logAdRevenue(logRevenue, additionalParams);
    }
}
#endif

using System.Collections.Generic;
using FirebaseModules.Analytics;

public class AdRevenueInfo
{
    public string AdNetwork;
    public string AdUnit;
    public string AdPlacement;
    public string CountryCode;
    public string AdFormat;
    public double AdRevenue;
}

public class FirebaseTraking
{
    public static void LogAdRevenue(AdRevenueInfo adInfo)
    {
        var parameters = new Dictionary<string, object>
        {
            { "ad_platform", "AppLovin" },
            { "ad_source", adInfo.AdNetwork },
            { "ad_unit_name", adInfo.AdUnit },
            { "currency", "USD" },
            { "value", adInfo.AdRevenue },
            { "placement", adInfo.AdPlacement },
            { "country_code", adInfo.CountryCode },
            { "ad_format", adInfo.AdFormat }
        };

        FirebaseAnalyticsService.LogEvent("ad_impression", parameters);
    }
}
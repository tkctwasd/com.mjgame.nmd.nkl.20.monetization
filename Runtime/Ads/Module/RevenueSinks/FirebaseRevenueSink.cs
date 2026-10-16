#if USE_FIREBASE
using Firebase.Analytics;
using UnityEngine;

public class FirebaseRevenueSink : IAdRevenueSink
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register() => AdRevenueTracker.Register(new FirebaseRevenueSink());

    public void Send(AdRevenueData data)
    {
        Parameter[] adParameters = {
            new Parameter("ad_platform", "Applovin"),
            new Parameter("ad_source", data.NetworkName),
            new Parameter("ad_unit_name", data.AdUnitId),
            new Parameter("currency", "USD"),
            new Parameter("value", data.Revenue),
            new Parameter("placement", data.Placement ?? ""),
            new Parameter("country_code", data.CountryCode),
            new Parameter("ad_format", data.AdFormat)
        };
        FirebaseAnalytics.LogEvent("ad_impression", adParameters);
    }
}
#endif

using Firebase.Analytics;
using UnityEngine;

public class FirebaseTracking : ITracking
{
    public bool IsInitialized { get; set; }
    [RuntimeInitializeOnLoadMethod]
    static void InitFirebaseTracking()
    {
        var logService = new FirebaseTracking();
        ServiceTracking.Register(logService);
    }
    public void Initialize()
    {
        Wasd.LogStateInitialize("FirebaseTracking");
    }
    public void SetInfo() { }
    public void OnAdsRevenue(AdRevenueInfo adInfo)
    {
        Parameter[] AdParameters = {
                new Parameter("ad_platform", "Applovin"),
                new Parameter("ad_source", adInfo.AdNetwork),
                new Parameter("ad_unit_name", adInfo.AdUnit),
                new Parameter("currency","USD"),
                new Parameter("value", adInfo.AdRevenue),
                new Parameter("placement", adInfo.AdPlacement),
                new Parameter("country_code", adInfo.CountryCode),
                new Parameter("ad_format", adInfo.AdFormat)
            };

        FirebaseAnalytics.LogEvent("ad_impression", AdParameters);

    }
}

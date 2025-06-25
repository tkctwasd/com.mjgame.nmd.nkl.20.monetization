namespace A
{
    #if USE_ADJUST
using AdjustSdk;
#endif
#if USE_APPSFLYER
using AppsFlyerSDK;
#endif
using Firebase.Analytics;
using System.Collections.Generic;
using UnityEngine;
using static MaxSdkBase; // Cần MaxSdkBase để sử dụng AdInfo

public class AdRevenueTracker
{
    // Constructor (Nếu cần khởi tạo với tham số nào đó, ở đây thì không)
    public AdRevenueTracker()
    {
        LogW.Log("AdRevenueTracker initialized.", "AdRevenueTracker");
    }

    // Phương thức chính để xử lý dữ liệu doanh thu quảng cáo
    public void TrackAdRevenue(AdInfo impressionData)
    {
        LogW.Log($"Tracking Ad Revenue: {impressionData.Revenue} from {impressionData.NetworkName} for {impressionData.AdUnitIdentifier}", "AdRevenueTracker");
        SendRevenueToFirebase(impressionData);
        SendRevenueToAppsflyer(impressionData);
        // SendRevenueToAdjust(impressionData); // Bỏ comment khi bạn có AdjustManager
    }

    // Gửi dữ liệu doanh thu quảng cáo lên Firebase Analytics
    private void SendRevenueToFirebase(AdInfo adInfo)
    {
        Parameter[] AdParameters = {
            new Parameter("ad_platform", "Applovin"),
            new Parameter("ad_source", adInfo.NetworkName),
            new Parameter("ad_unit_name", adInfo.AdUnitIdentifier),
            new Parameter("currency","USD"), // Tiền tệ mặc định là USD
            new Parameter("value", adInfo.Revenue),
            new Parameter("placement", adInfo.Placement), // Đây là placement từ MAX, không phải placement bạn tự định nghĩa
            new Parameter("country_code", adInfo.NetworkPlacement), // Có thể là country code hoặc network placement
            new Parameter("ad_format", adInfo.AdFormat)
        };

        FirebaseAnalytics.LogEvent("ad_impression", AdParameters);
        LogW.Log($"Sent revenue to Firebase: {adInfo.Revenue}", "Firebase Revenue");
    }

    // Gửi dữ liệu doanh thu quảng cáo lên AppsFlyer (chỉ khi USE_APPSFLYER được định nghĩa)
    private void SendRevenueToAppsflyer(AdInfo adInfo)
    {
    #if USE_APPSFLYER
        LogW.Log("AppsflyerRevenue: " + Time.time + " " + adInfo.Revenue);
        Dictionary<string, string> additionalParams = new Dictionary<string, string>();
        additionalParams.Add(AdRevenueScheme.COUNTRY, adInfo.NetworkPlacement);
        additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnitIdentifier);
        additionalParams.Add(AdRevenueScheme.AD_TYPE, adInfo.AdFormat);
        additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.Placement);
        var logRevenue = new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
        AppsFlyer.logAdRevenue(logRevenue, additionalParams);
        LogW.Log($"Sent revenue to AppsFlyer: {adInfo.Revenue}", "AppsFlyer Revenue");
    #endif
    }

    // Gửi dữ liệu doanh thu quảng cáo lên Adjust (chỉ khi USE_ADJUST được định nghĩa)
    // private void SendRevenueToAdjust(AdInfo adInfo) // Bỏ comment khi bạn có AdjustManager
    // {
    // #if USE_ADJUST
    //     LogW.Log("AdjustRevenue " + Time.time + " " + adInfo.Revenue);
    //     var adRevenue = new AdjustAdRevenue("applovin_max_sdk");
    //     adRevenue.SetRevenue(adInfo.Revenue, "USD");
    //     adRevenue.AdRevenueNetwork = adInfo.NetworkName;
    //     adRevenue.AdRevenueUnit = adInfo.AdUnitIdentifier;
    //     adRevenue.AdRevenuePlacement = adInfo.Placement;
    //     Adjust.TrackAdRevenue(adRevenue);
    // #endif
    // }
}
}
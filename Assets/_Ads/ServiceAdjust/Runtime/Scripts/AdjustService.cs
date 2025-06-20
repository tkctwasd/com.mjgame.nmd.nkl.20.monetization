using AdjustSdk;
using UnityEngine;

public class AdjustService : ITracking
{
    public bool IsInitialized { get; set; }
    string token;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        var logService = new AdjustService();
        ServiceTracking.Register(logService);
    }

    public void SetInfo()
    {
        if(IsInitialized)
        {
            return;
        }
        IsInitialized = true;
        Wasd.Log("Adjust SetInfo");
#if UNITY_ANDROID && UNITY_2019_2_OR_NEWER
        Application.deepLinkActivated += (deeplink) =>
        {
            Adjust.ProcessDeeplink(new AdjustDeeplink(deeplink));
        };
        if(!string.IsNullOrEmpty(Application.absoluteURL))
        {
            // cold start and Application.absoluteURL not null so process deep link
            Adjust.ProcessDeeplink(new AdjustDeeplink(Application.absoluteURL));
        }
#endif
        AdjustConfig adjustConfig = new AdjustConfig(token, AdjustEnvironment.Production,
          false);
        adjustConfig.LogLevel = AdjustLogLevel.Info;
        adjustConfig.IsSendingInBackgroundEnabled = false;
        adjustConfig.IsDeferredDeeplinkOpeningEnabled = true;
        adjustConfig.IsCoppaComplianceEnabled = false;
        adjustConfig.IsCostDataInAttributionEnabled = false;
        adjustConfig.IsPreinstallTrackingEnabled = false;
        adjustConfig.IsPlayStoreKidsComplianceEnabled = false;
        adjustConfig.IsAdServicesEnabled = true;
        adjustConfig.IsIdfaReadingEnabled = true;
        adjustConfig.IsLinkMeEnabled = false;
        adjustConfig.IsSkanAttributionEnabled = true;
        Adjust.InitSdk(adjustConfig);
    }
    public void Initialize()
    {
        Wasd.LogStateInitialize("Adjust");
        token = API.Info.GetValue(ServiceParameters.ADJUST_APP_TOKEN).ToString();
        if(!Application.isEditor)
        {
            Wasd.Log($"adjust token (is encrypt): {token}");
        }
        else
        {
            Wasd.Log($"adjust token: {token}");
        }
    }

    public void OnAdsRevenue(AdRevenueInfo adInfo)
    {
        Wasd.Log("AdjustRevenue " + Time.time + " " + adInfo.AdRevenue);
        var adRevenue = new AdjustAdRevenue("applovin_max_sdk");
        adRevenue.SetRevenue(adInfo.AdRevenue, "USD");
        adRevenue.AdRevenueNetwork = adInfo.AdNetwork;
        adRevenue.AdRevenueUnit = adInfo.AdUnit;
        adRevenue.AdRevenuePlacement = adInfo.AdPlacement;
        Adjust.TrackAdRevenue(adRevenue);
    }
}
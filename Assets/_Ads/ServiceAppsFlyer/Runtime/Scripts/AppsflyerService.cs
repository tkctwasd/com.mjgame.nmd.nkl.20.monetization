using AppsFlyerSDK;
using System.Collections.Generic;
using UnityEngine;

public class AppsflyerService : ITracking
{
    public bool IsInitialized { get; set; }
    [RuntimeInitializeOnLoadMethod]
    static void InitAppsflyer()
    {
        var logService = new AppsflyerService();
        ServiceTracking.Register(logService);
    }

    public void Initialize()
    {
        Wasd.LogStateInitialize("Appsflyer");
#if UNITY_IOS && !UNITY_EDITOR
    AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif
    }
    public void SetInfo()
    {
        Wasd.Log("Appsflyer SetInfo");
        if(IsInitialized)
        {
            return;
        }
        IsInitialized = true;
        string devKey = API.Info.GetValue(ServiceParameters.APPSFLYER_DEV_KEY).ToString();
        string appID = API.Info.GetValue(ServiceParameters.APPSFLYER_APP_ID).ToString();
        AppsFlyer.initSDK(devKey, appID, null);
        AppsFlyer.startSDK();
        AppsFlyer.enableTCFDataCollection(true);

        if(!Application.isEditor)
        {
            Wasd.LogColor($"Appsflyer (is encrypt): DevKey {devKey}/AppID {appID}", "gray");
        }
        else
        {
            Wasd.Log($"Appsflyer: DevKey {devKey}/ AppID {appID}");
        }
    }

    public void OnAdsRevenue(AdRevenueInfo adInfo)
    {
        Wasd.Log("AppsflyerRevenue: " + adInfo.AdNetwork + " " + adInfo.AdRevenue);
        Dictionary<string, string> additionalParams = new Dictionary<string, string>();
        additionalParams.Add(AdRevenueScheme.COUNTRY, adInfo.CountryCode);
        additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnit);
        additionalParams.Add(AdRevenueScheme.AD_TYPE, adInfo.AdFormat);
        additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.AdPlacement);
        var logRevenue = new AFAdRevenueData(adInfo.AdNetwork, MediationNetwork.ApplovinMax, "USD", adInfo.AdRevenue);
        AppsFlyer.logAdRevenue(logRevenue, additionalParams);
    }
}
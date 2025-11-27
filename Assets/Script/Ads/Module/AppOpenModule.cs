using System;
using UnityEngine;

using static MaxSdkBase;

public class AppOpenModule
{
    public AdsManager ApplovinManager;
    private string appOpenId = "app-open-id";

    #region App Open 


    public AppOpenModule(AdsManager applovin, string appOpenId)
    {
        ApplovinManager = applovin;
        this.appOpenId = appOpenId;
        ApplovinManager.TimeCloseAOA = DateTime.MinValue;
        ApplovinManager.isLogEventLoading = false;
        ApplovinManager.isCanShowAOABackGame = true;
    }

    public void Initialize()
    {
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
        MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickEvent;
        MaxSdk.LoadAppOpenAd(appOpenId);
    }

    private void OnAppOpenAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Displayed"));
        ApplovinManager.isLogEventLoading = false;
        if (!ApplovinManager.IsShowFirstAOA) ApplovinManager.IsShowFirstAOA = true;
    }
    private void OnAppOpenAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Display Failed: {errorInfo.Message}"));
        ApplovinManager.isLogEventLoading = false;
        // tai lai quang cao
        MaxSdk.LoadAppOpenAd(appOpenId);
    }

    private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Hidden"));
        MaxSdk.LoadAppOpenAd(appOpenId);
        ApplovinManager.appOpenLocation = "";
        if (!ApplovinManager.IsCloseFirstAOA) ApplovinManager.IsCloseFirstAOA = true;
        ApplovinManager.TimeCloseAOA = DateTime.UtcNow;
    }

    private void OnAppOpenAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Loaded"));
    }

    private void OnAppOpenAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Load Failed: {errorInfo.Message}"));
    }

    private void OnAppOpenAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Clicked"));
        if (!string.IsNullOrEmpty(ApplovinManager.appOpenLocation))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_click", ApplovinManager.appOpenLocation);
        }
    }

    public bool IsShowAOA()
    {
        return RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowAppOpen && ApplovinManager.isCanShowAOABackGame;
    }
    public void ShowAppOpen(bool isLoading)
    {
        if (!IsShowAOA()) return;
        bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.TimeCloseAOA).TotalSeconds >= RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).AppOpenCappingTime;

        ApplovinManager.appOpenLocation = isLoading ? "loading" : "resume";

        if ((isLoading || passedCappingTime) && IsShowAOA())
        {
            if ((!ApplovinManager.isLogEventLoading && isLoading) || !isLoading)
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads", ApplovinManager.appOpenLocation);
                ApplovinManager.isLogEventLoading = true;
            }
            if (MaxSdk.IsAppOpenAdReady(appOpenId))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_success", ApplovinManager.appOpenLocation);
                MaxSdk.ShowAppOpenAd(appOpenId);
            }
            else
            {
                MaxSdk.LoadAppOpenAd(appOpenId);
            }
        }
    }
    #endregion

    #region RevenuePaidEvent
    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        ApplovinManager.SendAdRevenueTracker(impressionData);
    }

    #endregion
}

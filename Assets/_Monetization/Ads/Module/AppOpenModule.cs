using System;
using UnityEngine;

using static MaxSdkBase;

public class AppOpenModule
{
    public AdsManager ApplovinManager;
    private string appOpenId = "app-open-id";
    internal string appOpenLocation="";
    internal bool isLogEventLoading;


    #region App Open 


    public AppOpenModule(AdsManager applovin, string appOpenId)
    {
        ApplovinManager = applovin;
        this.appOpenId = appOpenId;
        isLogEventLoading = false;
        ApplovinManager.IsCanShowAOABackGame = true;
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


    public bool CanShowAOA(bool isLoading)
    {
        var config = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (config == null || !config.ShowAppOpen) return false;

        if (!ApplovinManager.IsCanShowAOABackGame) return false;

        if (!isLoading)
        {
            double elapsed = (DateTime.UtcNow - ApplovinManager.LastTimeShowAOA).TotalSeconds;
            if (elapsed < config.AppOpenCappingTime) return false;
        }

        if (!MaxSdk.IsAppOpenAdReady(appOpenId))
        {
            MaxSdk.LoadAppOpenAd(appOpenId);
            return false;
        }

        return true;
    }
    public void ShowAppOpen(bool isLoading)
    {
        appOpenLocation = isLoading ? "loading" : "resume";

        if (CanShowAOA(isLoading))
        {
            if (!isLoading || !isLogEventLoading)
            {
                AdAnalytic.LogAd("show_app_open_ads", appOpenLocation);
                if (isLoading) isLogEventLoading = true;
            }

            AdAnalytic.LogAd("show_app_open_ads_success", appOpenLocation);

            ApplovinManager.IsShowAds = true;
            MaxSdk.ShowAppOpenAd(appOpenId);
        }
        else
        {
            ApplovinManager.callAOADismissed?.Invoke();
        }
    }
    private void OnAppOpenAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Displayed"));
        isLogEventLoading = false;
        if (!ApplovinManager.IsShowFirstAOA) ApplovinManager.IsShowFirstAOA = true;
    }
    private void OnAppOpenAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Display Failed: {errorInfo.Message}"));
        isLogEventLoading = false;

        ApplovinManager.IsShowAds = false;

        MaxSdk.LoadAppOpenAd(appOpenId);
        ApplovinManager.callAOADismissed?.Invoke();
    }

    private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("AppOpen".Ads($"Ad Hidden"));
        MaxSdk.LoadAppOpenAd(appOpenId);
        if ("resume".Equals(appOpenLocation))
        {
            ApplovinManager.callAOADismissed?.Invoke();
        }
        appOpenLocation = "";
        if (!ApplovinManager.IsCloseFirstAOA) ApplovinManager.IsCloseFirstAOA = true;
        ApplovinManager.LastTimeShowAOA = DateTime.UtcNow;
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
        if (!string.IsNullOrEmpty(appOpenLocation))
        {
            AdAnalytic.LogAd("show_app_open_ads_click", appOpenLocation);
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

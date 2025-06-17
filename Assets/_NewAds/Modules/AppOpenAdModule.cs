using System;
using UnityEngine;

public class AppOpenAdModule
{
    private AdsRemoteConfig config;
    private IAdsTracker tracker;
    private IAdsAnalytics analytics;
    private bool isLoaded;
    private float lastShowTime;
    private bool isLoading;

    public event Action OnAdLoaded;
    public event Action OnAdFailedToLoad;
    public event Action OnAdShown;
    public event Action OnAdClosed;
    public event Action OnAdFailedToShow;

    public AppOpenAdModule(AdsRemoteConfig config, IAdsTracker tracker, IAdsAnalytics analytics)
    {
        this.config = config;
        this.tracker = tracker;
        this.analytics = analytics;
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += (adUnitId, adInfo) => {
            isLoaded = true;
            isLoading = false;
            tracker.TrackImpression(adUnitId, adInfo.NetworkName, "appopen");
            analytics.LogEvent("appopen_loaded");
            OnAdLoaded?.Invoke();
        };
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
            isLoaded = false;
            isLoading = false;
            analytics.LogEvent("appopen_failed_to_load", errorInfo);
            OnAdFailedToLoad?.Invoke();
        };
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += (adUnitId, adInfo) => {
            lastShowTime = Time.unscaledTime;
            tracker.TrackEvent("appopen_shown");
            analytics.LogEvent("appopen_shown");
            OnAdShown?.Invoke();
        };
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += (adUnitId, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("appopen_closed");
            OnAdClosed?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("appopen_failed_to_show", errorInfo);
            OnAdFailedToShow?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adUnitId, adInfo) => {
            tracker.TrackRevenue(adUnitId, adInfo.NetworkName, adInfo.Revenue, "USD", "appopen");
        };
        MaxSdkCallbacks.AppOpen.OnAdClickedEvent += (adUnitId, adInfo) => {
            tracker.TrackClick(adUnitId, adInfo.NetworkName, "appopen");
        };
    }

    public void Preload()
    {
        if (!config.EnableAppOpen || isLoading) return;
        isLoading = true;
        MaxSdk.LoadAppOpenAd(config.AppOpenId);
    }

    public bool CanShow()
    {
        return isLoaded && (Time.unscaledTime - lastShowTime > config.AppOpenCooldown);
    }

    public void Show()
    {
        if (!CanShow()) return;
        MaxSdk.ShowAppOpenAd(config.AppOpenId);
    }
}
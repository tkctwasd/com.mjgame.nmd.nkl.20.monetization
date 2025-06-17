using System;
using UnityEngine;

public class BannerAdModule
{
    private AdsRemoteConfig config;
    private IAdsTracker tracker;
    private IAdsAnalytics analytics;
    private bool isLoaded;
    private bool isLoading;
    private bool isShowing;

    public event Action OnAdLoaded;
    public event Action OnAdFailedToLoad;
    public event Action OnAdShown;
    public event Action OnAdHidden;
    public event Action OnAdClicked;

    public BannerAdModule(AdsRemoteConfig config, IAdsTracker tracker, IAdsAnalytics analytics)
    {
        this.config = config;
        this.tracker = tracker;
        this.analytics = analytics;
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adUnitId, adInfo) => {
            isLoaded = true;
            isLoading = false;
            tracker.TrackImpression(adUnitId, adInfo.NetworkName, "banner");
            analytics.LogEvent("banner_loaded");
            OnAdLoaded?.Invoke();
        };
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
            isLoaded = false;
            isLoading = false;
            analytics.LogEvent("banner_failed_to_load", errorInfo);
            OnAdFailedToLoad?.Invoke();
        };
        MaxSdkCallbacks.Banner.OnAdClickedEvent += (adUnitId, adInfo) => {
            tracker.TrackClick(adUnitId, adInfo.NetworkName, "banner");
            analytics.LogEvent("banner_clicked");
            OnAdClicked?.Invoke();
        };
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitId, adInfo) => {
            tracker.TrackRevenue(adUnitId, adInfo.NetworkName, adInfo.Revenue, "USD", "banner");
        };
    }

    public void Preload()
    {
        if (!config.EnableBanner || isLoading) return;
        isLoading = true;
        var bannerConfig = new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter);
        MaxSdk.CreateBanner(config.BannerId, bannerConfig);
        MaxSdk.SetBannerBackgroundColor(config.BannerId, Color.clear);
        MaxSdk.LoadBanner(config.BannerId);
    }

    public void Show()
    {
        if (!isLoaded || isShowing) return;
        MaxSdk.ShowBanner(config.BannerId);
        isShowing = true;
        tracker.TrackEvent("banner_shown");
        analytics.LogEvent("banner_shown");
        OnAdShown?.Invoke();
    }

    public void Hide()
    {
        if (!isShowing) return;
        MaxSdk.HideBanner(config.BannerId);
        isShowing = false;
        tracker.TrackEvent("banner_hidden");
        analytics.LogEvent("banner_hidden");
        OnAdHidden?.Invoke();
    }
}
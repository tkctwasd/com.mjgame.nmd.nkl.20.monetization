using System;
using UnityEngine;

public class InterstitialAdModule
{
    private AdsRemoteConfig config;
    private IAdsTracker tracker;
    private IAdsAnalytics analytics;
    private bool isLoaded;
    private bool isLoading;
    private float lastShowTime;

    public event Action OnAdLoaded;
    public event Action OnAdFailedToLoad;
    public event Action OnAdShown;
    public event Action OnAdClosed;
    public event Action OnAdFailedToShow;
    public event Action OnAdClicked;

    public InterstitialAdModule(AdsRemoteConfig config, IAdsTracker tracker, IAdsAnalytics analytics)
    {
        this.config = config;
        this.tracker = tracker;
        this.analytics = analytics;
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adUnitId, adInfo) => {
            isLoaded = true;
            isLoading = false;
            tracker.TrackImpression(adUnitId, adInfo.NetworkName, "interstitial");
            analytics.LogEvent("interstitial_loaded");
            OnAdLoaded?.Invoke();
        };
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
            isLoaded = false;
            isLoading = false;
            analytics.LogEvent("interstitial_failed_to_load", errorInfo);
            OnAdFailedToLoad?.Invoke();
        };
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adUnitId, adInfo) => {
            lastShowTime = Time.unscaledTime;
            tracker.TrackEvent("interstitial_shown");
            analytics.LogEvent("interstitial_shown");
            OnAdShown?.Invoke();
        };
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adUnitId, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("interstitial_closed");
            OnAdClosed?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("interstitial_failed_to_show", errorInfo);
            OnAdFailedToShow?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (adUnitId, adInfo) => {
            tracker.TrackClick(adUnitId, adInfo.NetworkName, "interstitial");
            analytics.LogEvent("interstitial_clicked");
            OnAdClicked?.Invoke();
        };
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnitId, adInfo) => {
            tracker.TrackRevenue(adUnitId, adInfo.NetworkName, adInfo.Revenue, "USD", "interstitial");
        };
    }

    public void Preload()
    {
        if (!config.EnableInterstitial || isLoading) return;
        isLoading = true;
        MaxSdk.LoadInterstitial(config.InterstitialId);
    }

    public bool CanShow()
    {
        return isLoaded && (Time.unscaledTime - lastShowTime > config.InterstitialCooldown);
    }

    public void Show()
    {
        if (!CanShow()) return;
        MaxSdk.ShowInterstitial(config.InterstitialId);
    }
}
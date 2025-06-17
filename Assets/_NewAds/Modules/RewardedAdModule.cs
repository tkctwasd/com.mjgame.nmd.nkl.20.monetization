using System;
using UnityEngine;

public class RewardedAdModule
{
    private AdsRemoteConfig config;
    private IAdsTracker tracker;
    private IAdsAnalytics analytics;
    private bool isLoaded;
    private bool isLoading;

    public event Action OnAdLoaded;
    public event Action OnAdFailedToLoad;
    public event Action OnAdShown;
    public event Action OnAdClosed;
    public event Action OnAdFailedToShow;
    public event Action OnAdClicked;
    public event Action OnUserRewarded;

    public RewardedAdModule(AdsRemoteConfig config, IAdsTracker tracker, IAdsAnalytics analytics)
    {
        this.config = config;
        this.tracker = tracker;
        this.analytics = analytics;
        RegisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adUnitId, adInfo) => {
            isLoaded = true;
            isLoading = false;
            tracker.TrackImpression(adUnitId, adInfo.NetworkName, "rewarded");
            analytics.LogEvent("rewarded_loaded");
            OnAdLoaded?.Invoke();
        };
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adUnitId, errorInfo) => {
            isLoaded = false;
            isLoading = false;
            analytics.LogEvent("rewarded_failed_to_load", errorInfo);
            OnAdFailedToLoad?.Invoke();
        };
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adUnitId, adInfo) => {
            tracker.TrackEvent("rewarded_shown");
            analytics.LogEvent("rewarded_shown");
            OnAdShown?.Invoke();
        };
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adUnitId, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("rewarded_closed");
            OnAdClosed?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => {
            isLoaded = false;
            analytics.LogEvent("rewarded_failed_to_show", errorInfo);
            OnAdFailedToShow?.Invoke();
            Preload();
        };
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (adUnitId, adInfo) => {
            tracker.TrackClick(adUnitId, adInfo.NetworkName, "rewarded");
            analytics.LogEvent("rewarded_clicked");
            OnAdClicked?.Invoke();
        };
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitId, adInfo) => {
            tracker.TrackRevenue(adUnitId, adInfo.NetworkName, adInfo.Revenue, "USD", "rewarded");
        };
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adUnitId, reward, adInfo) => {
            analytics.LogEvent("rewarded_user_rewarded");
            OnUserRewarded?.Invoke();
        };
    }

    public void Preload()
    {
        if (!config.EnableRewarded || isLoading) return;
        isLoading = true;
        MaxSdk.LoadRewardedAd(config.RewardedId);
    }

    public bool CanShow()
    {
        return isLoaded;
    }

    public void Show()
    {
        if (!CanShow()) return;
        MaxSdk.ShowRewardedAd(config.RewardedId);
    }
}
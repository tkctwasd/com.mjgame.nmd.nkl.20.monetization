namespace A
{
    using System;
using UnityEngine;
using static MaxSdkBase;

public class InterstitialAdManager
{
    // NEW: Tham chiếu đến ApplovinMaxManager
    private ApplovinMaxManager _applovinMaxManager;
    private string _adUnitId;
    private int _retryAttempt;
    private string _interstitialLocation;

    private Action _onInterstitialFinishedCallback;
    private DateTime _lastInterstitialShowTime = DateTime.MinValue;

    // Actions để thông báo cho lớp điều phối (ApplovinMaxManager)
    public Action OnAdDisplayed;
    public Action OnAdHidden;

    // Constructor: Khởi tạo Manager với tham chiếu và Ad Unit ID
    public InterstitialAdManager(ApplovinMaxManager applovinManager, string adUnitId)
    {
        _applovinMaxManager = applovinManager; // Gán tham chiếu
        _adUnitId = adUnitId;
    }

    public void Initialize()
    {
        LogW.Log($"Initializing Interstitial Ad with ID: {_adUnitId}", "Interstitial");

        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialAdLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialAdLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdDisplayFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAdClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialAdHiddenEvent;

        LoadInterstitial();
    }

    private bool IsInterstitialEnabledByRemoteConfig()
    {
        return RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Interstitial");
    }

    public bool IsInterstitialReady()
    {
        return MaxSdk.IsInterstitialReady(_adUnitId);
    }

    public void LoadInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(_adUnitId))
        {
            LogW.Log("Interstitial Ad is already loaded.", "Interstitial");
            return;
        }

        LogW.Log("Loading Interstitial Ad...", "Interstitial");
        MaxSdk.LoadInterstitial(_adUnitId);
    }

    public void ShowInterstitial(string placement, Action callBack)
    {
        _interstitialLocation = placement;
        _onInterstitialFinishedCallback = callBack;

        if (!IsInterstitialEnabledByRemoteConfig())
        {
            LogW.Log("Interstitial Ad is disabled by Remote Config. Invoking callback.", "Interstitial");
            _onInterstitialFinishedCallback?.Invoke();
            return;
        }

        int interval = RemoteConfigManager.GetConfigValue<int>("Ad_Interval_Show_Interstitial");
        bool passedCappingTime = (DateTime.UtcNow - _lastInterstitialShowTime).TotalSeconds >= interval;

        LogW.Log($"Attempting to show Interstitial Ad at {placement}. IsReady: {IsInterstitialReady()}, PassedCappingTime: {passedCappingTime}", "Interstitial");
        FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads", placement);

        if (IsInterstitialReady() && passedCappingTime)
        {
            LogW.Log($"Showing Interstitial Ad from {placement}", "Interstitial");
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads_success", placement);
            MaxSdk.ShowInterstitial(_adUnitId);
        }
        else
        {
            LogW.Log($"Interstitial Ad not shown. Ready: {IsInterstitialReady()}, Capping: {passedCappingTime}. Invoking callback.", "Interstitial");
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads_not_ready_or_capped", placement);
            LoadInterstitial();
            _onInterstitialFinishedCallback?.Invoke();
            _onInterstitialFinishedCallback = null;
        }
    }

    private void OnInterstitialAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Interstitial Ad loaded. Source: {adInfo.NetworkName}", "Interstitial");
        _retryAttempt = 0;
    }

    private void OnInterstitialAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        LogW.Log($"Interstitial Ad load failed: {errorInfo.Message}", "Interstitial");
        FirebaseAnalyticManager.Instance.LogAd("interstitial_load_fail", _interstitialLocation, errorInfo.Message);
        _retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt));
        LogW.Log($"Retrying Interstitial Ad load in {retryDelay} seconds...", "Interstitial");
    }

    private void OnInterstitialAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Interstitial Ad displayed. Source: {adInfo.NetworkName}", "Interstitial");
        _lastInterstitialShowTime = DateTime.UtcNow;
        OnAdDisplayed?.Invoke(); // Kích hoạt sự kiện
    }

    private void OnInterstitialAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Interstitial Ad display failed: {errorInfo.Message}", "Interstitial");
        FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads_fail", _interstitialLocation, errorInfo.Message);

        _onInterstitialFinishedCallback?.Invoke();
        _onInterstitialFinishedCallback = null;

        LoadInterstitial();
        OnAdHidden?.Invoke(); // Kích hoạt sự kiện
    }

    private void OnInterstitialAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Interstitial Ad clicked. Source: {adInfo.NetworkName}", "Interstitial");
        if (!string.IsNullOrEmpty(_interstitialLocation))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads_click", _interstitialLocation);
        }
    }

    private void OnInterstitialAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Interstitial Ad hidden", "Interstitial");
        _onInterstitialFinishedCallback?.Invoke();
        _onInterstitialFinishedCallback = null;

        LoadInterstitial();
        _interstitialLocation = "";
        OnAdHidden?.Invoke(); // Kích hoạt sự kiện
    }
}
}
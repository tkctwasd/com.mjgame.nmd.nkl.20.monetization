using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class InterstitialModule
{
    public AdsManager ApplovinManager;
    private string intersId = "inters-id";
    public InterstitialModule(AdsManager applovinMaxManager, string intersId)
    {
        ApplovinManager = applovinMaxManager;
        this.intersId = intersId;
    }

    #region Interstitial
    public void Initialize()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdDisplayFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += InterstitialOnAdClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdHiddenEvent;

        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
    }

    private IEnumerator IELoadDelay(float time)
    {
        yield return new WaitForSeconds(time);
        LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        if (MaxSdk.IsInitialized())
        {
            if (!MaxSdk.IsInterstitialReady(intersId))
            {
                MaxSdk.LoadInterstitial(intersId);
            }
        }
    }

    public bool CanShowInterstitial()
    {
        var config = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (!config.ShowInterstitial)
            return false;

        bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.TimeCloseInterstitial).TotalSeconds >= config.InterstitialCappingTime;

        return MaxSdk.IsInitialized() && MaxSdk.IsInterstitialReady(intersId) && passedCappingTime;
    }

    public bool IsTimestampInterReward()
    {
        return Math.Abs(Time.deltaTime - ApplovinManager.lastTimeShowReward) > RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).TimestampInterReward;
    }


    public void ShowInterstitial(string location, Action callBack)
    {
        if (!RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowInterstitial)
        {
            callBack?.Invoke();
            return;
        }

        ApplovinManager.interLocation = location;
        FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads", location);
        ApplovinManager.isCanShowAOABackGame = false;

        bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.TimeCloseInterstitial).TotalSeconds >= RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).InterstitialCappingTime;

        if (MaxSdk.IsInterstitialReady(intersId) && passedCappingTime)
        {
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_success", location);
            ApplovinManager.callBackInters = callBack;
            MaxSdk.ShowInterstitial(intersId);
        }
        else
        {
            LoadInterstitial();
            ApplovinManager.isCanShowAOABackGame = true;
            callBack?.Invoke();
        }
    }

    private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        ApplovinManager.intersRetry = 0;
        Debug.Log("Interstitial".Ads($"Ad Loaded"));
    }

    private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        ApplovinManager.intersRetry++;
        double retryDelay = Math.Pow(2, Math.Min(6, ApplovinManager.intersRetry));
        ApplovinManager.StartCoroutine(IELoadDelay((float)retryDelay));
        Debug.Log("Interstitial".Ads($"Ad Load Failed: {errorInfo.Message}"));
    }

    private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial".Ads($"Ad Displayed"));
    }

    private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        if (ApplovinManager.callBackInters != null)
        {
            ApplovinManager.callBackInters?.Invoke();
            ApplovinManager.callBackInters = null;
        }
        ApplovinManager.isCanShowAOABackGame = true;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        Debug.Log("Interstitial".Ads($"Ad Display Failed"));
    }

    private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial".Ads($"Ad Click"));
        if (!string.IsNullOrEmpty(ApplovinManager.interLocation))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_click", ApplovinManager.interLocation);
        }
    }

    private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (ApplovinManager.callBackInters != null)
        {
            ApplovinManager.callBackInters.Invoke();
            ApplovinManager.callBackInters = null;
            ApplovinManager.TimeCloseInterstitial = DateTime.UtcNow;
        }
        ApplovinManager.isCanShowAOABackGame = true;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        Debug.Log("Interstitial".Ads($"Ad Hidden"));
    }
    #endregion

    #region RevenuePaidEvent
    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        ApplovinManager.SendAdRevenueTracker(impressionData);
    }
    #endregion
}

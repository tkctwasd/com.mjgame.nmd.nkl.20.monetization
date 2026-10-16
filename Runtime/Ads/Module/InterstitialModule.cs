#if USE_APPLOVIN
using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class InterstitialModule
{
    public AdsManager ApplovinManager;
    private string intersId = "inters-id";
    internal Action callBackInters;
    internal int intersRetry;
    internal string interLocation;
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

    public bool CanShowInterstitial(bool popupClose = false)
    {
        if (ApplovinManager.IsRemoveAdsPurchased)
            return false;

        var config = AdsManager.GetAdsConfig();
        if (!config.ShowInterstitial)
            return false;
        if (!config.InterstitialOnClose && popupClose)
            return false;

        bool delayTime = (DateTime.UtcNow - ApplovinManager.TimeStart).TotalSeconds >= config.TimeDelayInterstitial;

        bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.LastTimeShowInterstitial).TotalSeconds >= config.InterstitialCappingTime;

        bool timestampInterReward = (DateTime.UtcNow - ApplovinManager.LastTimeShowReward).TotalSeconds >= config.TimestampInterReward;
        bool timestampInterAOA = (DateTime.UtcNow - ApplovinManager.LastTimeShowAOA).TotalSeconds >= config.TimestampInterAppOpen;

        return MaxSdk.IsInitialized() && MaxSdk.IsInterstitialReady(intersId) && passedCappingTime && delayTime && timestampInterAOA && timestampInterReward;
    }


    public void ShowInterstitial(string location, Action callBack, bool popupClose = false)
    {
        interLocation = location;
        AdAnalytic.LogAd("show_interstitial_ads", location);
        ApplovinManager.IsCanShowAOABackGame = false;

        if (CanShowInterstitial(popupClose))
        {
            AdAnalytic.LogAd("show_interstitial_success", location);
            callBackInters = callBack;
            ApplovinManager.IsShowAds = true;
            MaxSdk.ShowInterstitial(intersId, location);
        }
        else
        {
            LoadInterstitial();
            ApplovinManager.IsCanShowAOABackGame = true;
            callBack?.Invoke();
        }
    }

    private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        intersRetry = 0;
        Debug.Log("Interstitial".Ads($"Ad Loaded"));
    }

    private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        intersRetry++;
        double retryDelay = Math.Pow(2, Math.Min(6, intersRetry));
        ApplovinManager.StartCoroutine(IELoadDelay((float)retryDelay));
        Debug.Log("Interstitial".Ads($"Ad Load Failed: {errorInfo.Message}"));
    }

    private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial".Ads($"Ad Displayed"));
    }

    private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        if (callBackInters != null)
        {
            callBackInters?.Invoke();
            callBackInters = null;
        }
        ApplovinManager.IsShowAds = false;
        ApplovinManager.IsCanShowAOABackGame = true;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        Debug.Log("Interstitial".Ads($"Ad Display Failed"));
    }

    private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial".Ads($"Ad Click"));
        if (!string.IsNullOrEmpty(interLocation))
        {
            AdAnalytic.LogAd("show_interstitial_click", interLocation);
        }
    }

    private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        callBackInters?.Invoke();
        callBackInters = null;
        ApplovinManager.IsShowAds = false;
        ApplovinManager.LastTimeShowInterstitial = DateTime.UtcNow;
        ApplovinManager.LastTimeShowAOA = DateTime.UtcNow;
        ApplovinManager.IsCanShowAOABackGame = true;
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

#endif

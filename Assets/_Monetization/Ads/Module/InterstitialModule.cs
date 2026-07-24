using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class InterstitialModule
{
    public AdsManager ApplovinManager;

    private enum Tier { High, Medium, Main }
    private readonly string intersIdHigh;
    private readonly string intersIdMedium;
    private readonly string intersIdMain;
    internal Action callBackInters;
    internal int intersRetry;
    internal string interLocation;

    public InterstitialModule(AdsManager applovinMaxManager, string intersIdHigh, string intersIdMedium, string intersIdMain)
    {
        ApplovinManager = applovinMaxManager;
        this.intersIdHigh = intersIdHigh;
        this.intersIdMedium = intersIdMedium;
        this.intersIdMain = intersIdMain;
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

    // Begin (or restart) the waterfall from HIGH. Idempotent: does nothing if an ad is already ready.
    public void LoadInterstitial()
    {
        if (!MaxSdk.IsInitialized())
            return;
        if (IsAnyTierReady())
            return;

        intersRetry = 0;
        LoadTier(Tier.High);
    }

    private void LoadTier(Tier tier)
    {
        string id = GetId(tier);
        if (MaxSdk.IsInterstitialReady(id))
            return;
        MaxSdk.LoadInterstitial(id);
    }

    private string GetId(Tier tier)
    {
        switch (tier)
        {
            case Tier.High: return intersIdHigh;
            case Tier.Medium: return intersIdMedium;
            default: return intersIdMain;
        }
    }

    private bool TryGetTier(string adUnitId, out Tier tier)
    {
        if (adUnitId == intersIdHigh) { tier = Tier.High; return true; }
        if (adUnitId == intersIdMedium) { tier = Tier.Medium; return true; }
        if (adUnitId == intersIdMain) { tier = Tier.Main; return true; }
        tier = Tier.Main;
        return false;
    }

    private bool IsAnyTierReady()
    {
        return MaxSdk.IsInterstitialReady(intersIdHigh)
            || MaxSdk.IsInterstitialReady(intersIdMedium)
            || MaxSdk.IsInterstitialReady(intersIdMain);
    }

    // Highest available tier to show: HIGH -> MEDIUM -> MAIN.
    private string GetReadyTierId()
    {
        if (MaxSdk.IsInterstitialReady(intersIdHigh)) return intersIdHigh;
        if (MaxSdk.IsInterstitialReady(intersIdMedium)) return intersIdMedium;
        if (MaxSdk.IsInterstitialReady(intersIdMain)) return intersIdMain;
        return null;
    }

    public bool CanShowInterstitial()
    {
        if (ApplovinManager.IsRemoveAdsPurchased)
            return false;

        var config = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (!config.ShowInterstitial)
            return false;

        bool delayTime = (DateTime.UtcNow - ApplovinManager.TimeStart).TotalSeconds >= config.TimeDelayInterstitial;

        bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.LastTimeShowInterstitial).TotalSeconds >= config.InterstitialCappingTime;

        bool timestampInterReward = (DateTime.UtcNow - ApplovinManager.LastTimeShowReward).TotalSeconds >= config.TimestampInterReward;
        bool timestampInterAOA = (DateTime.UtcNow - ApplovinManager.LastTimeShowAOA).TotalSeconds >= config.TimestampInterAppOpen;

        return MaxSdk.IsInitialized() && IsAnyTierReady() && passedCappingTime && delayTime && timestampInterAOA && timestampInterReward;
    }


    public void ShowInterstitial(string location, Action callBack)
    {
        interLocation = location;
        AdAnalytic.LogAd("show_interstitial_ads", location);
        ApplovinManager.IsCanShowAOABackGame = false;

        if (CanShowInterstitial())
        {
            AdAnalytic.LogAd("show_interstitial_success", location);
            callBackInters = callBack;
            ApplovinManager.IsShowAds = true;
            MaxSdk.ShowInterstitial(GetReadyTierId());
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
        if (!TryGetTier(adUnitId, out _))
            return;

        // Got fill: stop the waterfall here, this tier will be shown.
        intersRetry = 0;
        Debug.Log("Interstitial".Ads($"Ad Loaded"));
    }

    private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        if (!TryGetTier(adUnitId, out Tier tier))
            return;

        Debug.Log("Interstitial".Ads($"Ad Load Failed: {errorInfo.Message}"));

        switch (tier)
        {
            case Tier.High:
                LoadTier(Tier.Medium);
                break;
            case Tier.Medium:
                LoadTier(Tier.Main);
                break;
            case Tier.Main:
                intersRetry++;
                if (intersRetry < 3)
                {
                    float retryDelay = Mathf.Pow(2, intersRetry);
                    ApplovinManager.StartCoroutine(IERetryCycle(retryDelay));
                }
                break;
        }
    }

    private IEnumerator IERetryCycle(float time)
    {
        yield return new WaitForSeconds(time);
        LoadTier(Tier.High);
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

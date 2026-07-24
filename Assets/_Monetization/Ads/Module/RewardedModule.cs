using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class RewardedModule
{
    public AdsManager ApplovinManager;

    private enum Tier { High, Medium, Main }
    private readonly string rewardIdHigh;
    private readonly string rewardIdMedium;
    private readonly string rewardIdMain;
    internal Action callBackReward;
    internal int rewardedRetry;
    internal string rewardLocation;

    #region Rewarded

    public RewardedModule(AdsManager applovinMaxManager, string rewardIdHigh, string rewardIdMedium, string rewardIdMain)
    {
        ApplovinManager = applovinMaxManager;
        this.rewardIdHigh = rewardIdHigh;
        this.rewardIdMedium = rewardIdMedium;
        this.rewardIdMain = rewardIdMain;
    }

    public void Initialize()
    {
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedOnAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedOnAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedOnAdDisplayFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedOnAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedOnAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedOnAdReceivedRewardEvent;

        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
    }

    private IEnumerator IELoadDelay(float time)
    {
        yield return new WaitForSeconds(time);
        LoadRewardedAd();
    }

    // Begin (or restart) the waterfall from HIGH. Idempotent: does nothing if an ad is already ready.
    private void LoadRewardedAd()
    {
        if (!MaxSdk.IsInitialized())
            return;
        if (IsAnyTierReady())
            return;

        rewardedRetry = 0;
        LoadTier(Tier.High);
    }

    private void LoadTier(Tier tier)
    {
        string id = GetId(tier);
        if (MaxSdk.IsRewardedAdReady(id))
            return;
        MaxSdk.LoadRewardedAd(id);
    }

    private string GetId(Tier tier)
    {
        switch (tier)
        {
            case Tier.High: return rewardIdHigh;
            case Tier.Medium: return rewardIdMedium;
            default: return rewardIdMain;
        }
    }

    private bool TryGetTier(string adUnitId, out Tier tier)
    {
        if (adUnitId == rewardIdHigh) { tier = Tier.High; return true; }
        if (adUnitId == rewardIdMedium) { tier = Tier.Medium; return true; }
        if (adUnitId == rewardIdMain) { tier = Tier.Main; return true; }
        tier = Tier.Main;
        return false;
    }

    private bool IsAnyTierReady()
    {
        return MaxSdk.IsRewardedAdReady(rewardIdHigh)
            || MaxSdk.IsRewardedAdReady(rewardIdMedium)
            || MaxSdk.IsRewardedAdReady(rewardIdMain);
    }

    // Highest available tier to show: HIGH -> MEDIUM -> MAIN.
    private string GetReadyTierId()
    {
        if (MaxSdk.IsRewardedAdReady(rewardIdHigh)) return rewardIdHigh;
        if (MaxSdk.IsRewardedAdReady(rewardIdMedium)) return rewardIdMedium;
        if (MaxSdk.IsRewardedAdReady(rewardIdMain)) return rewardIdMain;
        return null;
    }

    public void ShowRewarded(string location, Action callBack)
    {
        rewardLocation = location;
        ApplovinManager.IsCanShowAOABackGame = false;

        AdAnalytic.LogAd("show_rewarded_ads", location);
        var config = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (!config.ShowReward)
        {
            ApplovinManager.IsCanShowAOABackGame = true;
            callBack?.Invoke();
            return;
        }

        if (MaxSdk.IsInitialized() && IsAnyTierReady())
        {
            AdAnalytic.LogAd("show_rewarded_ads_sucess", location);
            callBackReward = callBack;
            ApplovinManager.IsShowAds = true;
            MaxSdk.ShowRewardedAd(GetReadyTierId());
        }
        else
        {
            Debug.Log("Rewarded".Ads("Rewarded ad not ready"));
            LoadRewardedAd();
            ApplovinManager.IsCanShowAOABackGame = true;
        }
    }

    private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (!TryGetTier(adUnitId, out _))
            return;

        // Got fill: stop the waterfall here, this tier will be shown.
        Debug.Log("Rewarded".Ads($"Ad Loaded"));
        rewardedRetry = 0;
    }

    private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        if (!TryGetTier(adUnitId, out Tier tier))
            return;

        Debug.Log("Rewarded".Ads($"Ad Load Failed + {errorInfo.Message}"));

        switch (tier)
        {
            case Tier.High:
                LoadTier(Tier.Medium);
                break;
            case Tier.Medium:
                LoadTier(Tier.Main);
                break;
            case Tier.Main:
                rewardedRetry++;
                if (rewardedRetry < 3)
                {
                    float retryDelay = Mathf.Pow(2, rewardedRetry);
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

    private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        ApplovinManager.IsShowAds = false;
        ApplovinManager.IsCanShowAOABackGame = true;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        Debug.Log("Rewarded".Ads($"Ad Display Failed + {errorInfo.Message}"));
    }

    private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Displayed"));
    }

    private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Clicked"));
        AdAnalytic.LogAd("show_rewarded_ads_click", rewardLocation);
    }

    private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Hidden"));
        ApplovinManager.LastTimeShowReward = DateTime.UtcNow;
        ApplovinManager.LastTimeShowAOA = DateTime.UtcNow;
        ApplovinManager.IsCanShowAOABackGame = true;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
    }

    private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        if (callBackReward != null)
        {
            callBackReward?.Invoke();
            callBackReward = null;
        }
        ApplovinManager.IsCanShowAOABackGame = true;
        AdAnalytic.LogAd("show_rewarded_ads_completed", rewardLocation);
        Debug.Log("Rewarded".Ads("Ad Completed"));
    }

    #endregion

    #region RevenuePaidEvent
    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        ApplovinManager.SendAdRevenueTracker(impressionData);
    }
    #endregion

}
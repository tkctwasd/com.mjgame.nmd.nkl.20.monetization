using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class RewardedModule
{
    public AdsManager ApplovinManager;
    private string rewardId = "reward-id";
    internal Action callBackReward;
    internal int rewardedRetry;
    internal string rewardLocation;

    #region Rewarded

    public RewardedModule(AdsManager applovinMaxManager, string rewardId)
    {
        ApplovinManager = applovinMaxManager;
        this.rewardId = rewardId;
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

    private void LoadRewardedAd()
    {
        if (MaxSdk.IsInitialized())
        {
            MaxSdk.LoadRewardedAd(rewardId);
        }
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

        if (MaxSdk.IsInitialized() && MaxSdk.IsRewardedAdReady(rewardId))
        {
            AdAnalytic.LogAd("show_rewarded_ads_sucess", location);
            callBackReward = callBack;
            ApplovinManager.IsShowAds = true;
            MaxSdk.ShowRewardedAd(rewardId);
        }
        else
        {
            Debug.Log("Rewarded".Ads("Rewarded ad not ready"));
            ApplovinManager.IsCanShowAOABackGame = true;
        }
    }

    private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads($"Ad Loaded"));
        rewardedRetry = 0;
    }

    private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        rewardedRetry++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetry));
        ApplovinManager.StartCoroutine(IELoadDelay((float)retryDelay));
        Debug.Log("Rewarded".Ads($"Ad Load Failed + {errorInfo.Message}"));
    }

    private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        ApplovinManager.IsShowAds = false;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        ApplovinManager.IsCanShowAOABackGame = true;
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
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        ApplovinManager.IsCanShowAOABackGame = true;
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
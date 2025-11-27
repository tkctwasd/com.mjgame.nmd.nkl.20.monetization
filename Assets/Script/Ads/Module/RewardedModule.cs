using System;
using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class RewardedModule
{
    public AdsManager ApplovinManager;
    private string rewardId = "reward-id";

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
            Debug.Log("Rewarded".Ads("Loading Rewarded Ad..."));
        }
    }

    public void ShowRewarded(string location, Action callBack)
    {
        ApplovinManager.rewardLocation = location;
        ApplovinManager.isCanShowAOABackGame = false;

        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads", location);

        // show reward
        if (!RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowReward)
        {
            Debug.Log("Rewarded".Ads("Rewarded ads are disabled in the config."));
            callBack?.Invoke();
            return;
        }
        if (MaxSdk.IsRewardedAdReady(rewardId))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_sucess", location);
            ApplovinManager.callBackReward = callBack;
            MaxSdk.ShowRewardedAd(rewardId);
            ApplovinManager.lastTimeShowReward = Time.time;
        }
    }

    private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads($"Ad Loaded"));
        ApplovinManager.rewardedRetry = 0;
    }

    private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        ApplovinManager.rewardedRetry++;
        double retryDelay = Math.Pow(2, Math.Min(6, ApplovinManager.rewardedRetry));
        ApplovinManager.Invoke(nameof(LoadRewardedAd), (float)retryDelay);
        Debug.Log("Rewarded".Ads($"Ad Load Failed + {errorInfo.Message}"));
    }

    private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        ApplovinManager.isCanShowAOABackGame = true;
        Debug.Log("Rewarded".Ads($"Ad Display Failed + {errorInfo.Message}"));
    }

    private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Displayed"));
    }

    private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Clicked"));
        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_click", ApplovinManager.rewardLocation);
    }

    private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded".Ads("Ad Hidden"));
        ApplovinManager.TimeCloseAOA = DateTime.UtcNow;
        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        ApplovinManager.isCanShowAOABackGame = true;
    }

    private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        if (ApplovinManager.callBackReward != null)
        {
            ApplovinManager.callBackReward?.Invoke();
            ApplovinManager.callBackReward = null;
        }
        ApplovinManager.isCanShowAOABackGame = true;
        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_completed", ApplovinManager.rewardLocation);
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
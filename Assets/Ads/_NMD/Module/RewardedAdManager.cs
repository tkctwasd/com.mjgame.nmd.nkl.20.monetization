namespace A
{
#if USE_ADJUST
using AdjustSdk;
using AppLovinMax.Scripts.IntegrationManager.Editor;

#endif
#if USE_APPSFLYER
    using AppsFlyerSDK;
#endif
#if UNITY_EDITOR
    using AppLovinMax.Scripts.IntegrationManager.Editor;
#endif
    using Firebase.Analytics;
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using static MaxSdkBase;
    using System.Collections;

    public class RewardedAdManager
    {
        public ApplovinMaxManager ApplovinManager;
        private string rewardId = "reward-id";

        #region Rewarded

        public RewardedAdManager(ApplovinMaxManager applovinMaxManager, string rewardId)
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
            ApplovinManager.rewardLocation = location;
            ApplovinManager.isCanShowAOABackGame = false;
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads", location);
            if (MaxSdk.IsRewardedAdReady(rewardId))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_sucess", location);
                ApplovinManager.callBackReward = callBack;
                MaxSdk.ShowRewardedAd(rewardId);
            }
        }

        private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Loaded", "Rewarded");
            ApplovinManager.rewardedRetry = 0;
        }

        private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            ApplovinManager.rewardedRetry++;
            double retryDelay = Math.Pow(2, Math.Min(6, ApplovinManager.rewardedRetry));
            ApplovinManager.Invoke(nameof(LoadRewardedAd), (float)retryDelay);
            LogW.Log($"Ad Load Failed + {errorInfo.Message}", "Rewarded");
        }

        private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
            ApplovinManager.isCanShowAOABackGame = true;
            LogW.Log($"Ad Display Failed + {errorInfo.Message}", "Rewarded");
        }

        private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Displayed", "Rewarded");
        }

        private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Clicked", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_click", ApplovinManager.rewardLocation);
        }

        private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Hidden", "Rewarded");
            ApplovinManager.timeCloseAOA = Time.unscaledTime;
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
            LogW.Log("Ad Completed", "Rewarded");
        }

        #endregion

        #region RevenuePaidEvent
        private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
        {
            ApplovinManager.SendAdRevenueTracker(impressionData);
        }
        #endregion

    }
}
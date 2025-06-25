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

    public class RewardedAdManager : MonoBehaviour
    {
        public ApplovinMaxManager ApplovinMaxManager;
        [SerializeField] string rewardId = "rewarded-id";

        [Header("Banner")]
        [SerializeField] private AdViewPosition bannerPosition = AdViewPosition.BottomCenter;

        private Action callBackReward;


        [Header("Rewarded")]
        private int rewardedRetry;

        private string rewardLocation;


        #region Rewarded

        public RewardedAdManager(ApplovinMaxManager applovinMaxManager)
        {
            ApplovinMaxManager = applovinMaxManager;
        }

        private void InitializeRewardedAds()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedOnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedOnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedOnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedOnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedOnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedOnAdReceivedRewardEvent;

            Invoke(nameof(LoadRewardedAd), 0.5f);
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
            isCanShowAOABackGame = false;
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads", rewardLocation);
            if (MaxSdk.IsRewardedAdReady(rewardId))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_sucess", rewardLocation);
                callBackReward = callBack;
                MaxSdk.ShowRewardedAd(rewardId);
            }
        }

        private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Loaded", "Rewarded");
            rewardedRetry = 0;
        }

        private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            rewardedRetry++;
            double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetry));
            Invoke(nameof(LoadRewardedAd), (float)retryDelay);
            LogW.Log($"Ad Load Failed + {errorInfo.Message}", "Rewarded");
        }

        private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            Invoke(nameof(LoadRewardedAd), 0.5f);
            isCanShowAOABackGame = true;
            LogW.Log($"Ad Display Failed + {errorInfo.Message}", "Rewarded");
        }

        private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Displayed", "Rewarded");
        }

        private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Clicked", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_click", rewardLocation);
        }

        private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log("Ad Hidden", "Rewarded");
            timeCloseAOA = Time.unscaledTime;
            Invoke(nameof(LoadRewardedAd), 0.5f);
            isCanShowAOABackGame = true;
        }

        private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (callBackReward != null)
            {
                callBackReward.Invoke();
                callBackReward = null;
            }
            isCanShowAOABackGame = true;
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_completed", rewardLocation);
            LogW.Log("Ad Completed", "Rewarded");
        }

        #endregion

        #region RevenuePaidEvent
        private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
        {
            sendRevenueMAXToFirebase(impressionData);
            sendRevenueMAXToAdjust(impressionData);
            sendRevenueMAXToAppsflyer(impressionData);
        }

        void sendRevenueMAXToFirebase(AdInfo adInfo)
        {
            Parameter[] AdParameters = {
                new Parameter("ad_platform", "Applovin"),
                new Parameter("ad_source", adInfo.NetworkName),
                new Parameter("ad_unit_name", adInfo.AdUnitIdentifier),
                new Parameter("currency","USD"),
                new Parameter("value", adInfo.Revenue),
                new Parameter("placement", adInfo.Placement),
                new Parameter("country_code", adInfo.NetworkPlacement),
                new Parameter("ad_format", adInfo.AdFormat)
            };

            FirebaseAnalytics.LogEvent("ad_impression", AdParameters);
        }

        void sendRevenueMAXToAdjust(AdInfo adInfo)
        {
#if USE_ADJUST
        LogW.Log("AdjustRevenue " + Time.time + " " + adInfo.Revenue);
        var adRevenue = new AdjustAdRevenue("applovin_max_sdk");
        adRevenue.SetRevenue(adInfo.Revenue, "USD");
        adRevenue.AdRevenueNetwork = adInfo.NetworkName;
        adRevenue.AdRevenueUnit = adInfo.AdUnitIdentifier;
        adRevenue.AdRevenuePlacement = adInfo.Placement;
        Adjust.TrackAdRevenue(adRevenue);
#endif
        }

        void sendRevenueMAXToAppsflyer(AdInfo adInfo)
        {
#if USE_APPSFLYER
            LogW.Log("AppsflyerRevenue: " + Time.time + " " + adInfo.Revenue);
            Dictionary<string, string> additionalParams = new Dictionary<string, string>();
            additionalParams.Add(AdRevenueScheme.COUNTRY, adInfo.NetworkPlacement);
            additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnitIdentifier);
            additionalParams.Add(AdRevenueScheme.AD_TYPE, adInfo.AdFormat);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.Placement);
            var logRevenue = new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
#endif
        }
        #endregion
    }


}
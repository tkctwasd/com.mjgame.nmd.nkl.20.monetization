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

    public class AppOpenModule
    {
        public ApplovinMaxManager ApplovinManager;
        private string appOpenId = "app-open-id";

        #region App Open 


        public AppOpenModule(ApplovinMaxManager applovin, string appOpenId)
        {
            ApplovinManager = applovin;
            this.appOpenId = appOpenId;
            ApplovinManager.TimeCloseAOA = DateTime.MinValue;
            ApplovinManager.isLogEventLoading = false;
            ApplovinManager.isCanShowAOABackGame = true;
        }

        public void Initialize()
        {
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickEvent;
            MaxSdk.LoadAppOpenAd(appOpenId);
        }
        private void OnAppOpenAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogMessage.Log($"Ad Displayed", "AppOpen");
            ApplovinManager.isLogEventLoading = false;
            if (!ApplovinManager.IsShowFirstAOA) ApplovinManager.IsShowFirstAOA = true;
        }
        private void OnAppOpenAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogMessage.Log($"Ad Display Failed: {errorInfo.Message}", "AppOpen");
            ApplovinManager.isLogEventLoading = false;
            // tai lai quang cao
            MaxSdk.LoadAppOpenAd(appOpenId);
        }

        private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogMessage.Log($"Ad Hidden", "AppOpen");
            MaxSdk.LoadAppOpenAd(appOpenId);
            ApplovinManager.appOpenLocation = "";
            if (!ApplovinManager.IsCloseFirstAOA) ApplovinManager.IsCloseFirstAOA = true;
            ApplovinManager.TimeCloseAOA = DateTime.UtcNow;
        }

        private void OnAppOpenAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogMessage.Log($"Ad Loaded", "AppOpen");
        }

        private void OnAppOpenAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogMessage.Log($"Ad Load Failed: {errorInfo.Message}", "AppOpen");
        }

        private void OnAppOpenAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogMessage.Log($"Ad Clicked", "AppOpen");
            if (!string.IsNullOrEmpty(ApplovinManager.appOpenLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_click", ApplovinManager.appOpenLocation);
            }
        }

        private bool IsShowAOA()
        {
            return RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowAppOpen && ApplovinManager.isCanShowAOABackGame;
        }
        public void ShowAppOpen(bool isLoading)
        {
            if (!IsShowAOA()) return;
            bool passedCappingTime = (DateTime.UtcNow - ApplovinManager.TimeCloseAOA).TotalSeconds >= RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).AppOpenCappingTime;

            ApplovinManager.appOpenLocation = isLoading ? "loading" : "resume";

            if ((isLoading || passedCappingTime) && IsShowAOA())
            {
                if ((!ApplovinManager.isLogEventLoading && isLoading) || !isLoading)
                {
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads", ApplovinManager.appOpenLocation);
                    ApplovinManager.isLogEventLoading = true;
                }
                if (MaxSdk.IsAppOpenAdReady(appOpenId))
                {
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_success", ApplovinManager.appOpenLocation);
                    MaxSdk.ShowAppOpenAd(appOpenId);
                }
                else
                {
                    MaxSdk.LoadAppOpenAd(appOpenId);
                }
            }
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
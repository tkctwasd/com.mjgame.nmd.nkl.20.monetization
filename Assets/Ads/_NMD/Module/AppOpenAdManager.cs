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

    public class AppOpenAdManager
    {
        public ApplovinMaxManager ApplovinMaxManager;

        [Header("Ad ID")]
        [SerializeField] string appOpenId = "app-open-id";

        [Header("AppOpen")]
        private float timeCloseAOA;
        private string appOpenLocation;

        internal bool IsCloseFirstAOA;
        private bool isCanShowAOABackGame;
        internal bool IsShowFirstAOA;
        private bool isLogEventLoading;
        public DateTime TimeCloseAOA { get; set; }


        #region App Open 
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                ShowAppOpen(false);
            }
        }

        private AppOpenAdManager(ApplovinMaxManager applovinMaxManager)
        {
            timeCloseAOA = -1000f;
            isCanShowAOABackGame = true;
        }

        private void InitializeAppOpen()
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
            LogW.Log($"Ad Displayed", "AppOpen");
            if (!IsShowFirstAOA) IsShowFirstAOA = true;
        }
        private void OnAppOpenAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Display Failed: {errorInfo.Message}", "AppOpen");
        }

        private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Hidden", "AppOpen");
            MaxSdk.LoadAppOpenAd(appOpenId);
            appOpenLocation = "";
            if (!IsCloseFirstAOA) IsCloseFirstAOA = true;
            TimeCloseAOA = DateTime.UtcNow;
        }

        private void OnAppOpenAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Loaded", "AppOpen");
        }

        private void OnAppOpenAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogW.Log($"Ad Load Failed: {errorInfo.Message}", "AppOpen");
        }

        private void OnAppOpenAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Clicked", "AppOpen");
            if (!string.IsNullOrEmpty(appOpenLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_click", appOpenLocation);
            }
        }

        private bool IsShowAOA()
        {
            return (RemoteConfigManager.GetConfigValue<bool>("Ad_Use_AppOpen") && isCanShowAOABackGame);
        }
        public void ShowAppOpen(bool isLoading)
        {
            bool passedCappingTime = DateTime.UtcNow.Subtract(TimeCloseAOA).TotalSeconds >= RemoteConfigManager.GetConfigValue<int>("Ad_Interval_Show_AppOpen");
            appOpenLocation = isLoading ? "loading" : "pause";
            if ((isLoading || passedCappingTime) && IsShowAOA())
            {
                if ((!isLogEventLoading && isLoading) || !isLoading)
                {
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads", appOpenLocation);
                    isLogEventLoading = true;
                }
                if (MaxSdk.IsAppOpenAdReady(appOpenId))
                {
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_success", appOpenLocation);
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
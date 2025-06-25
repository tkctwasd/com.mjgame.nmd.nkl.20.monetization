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

    public class BannerAdManager
    {
        ApplovinMaxManager ApplovinMaxManager;
        [SerializeField] string bannerId = "banner-id";

        [Header("Banner")]
        [SerializeField] private AdViewPosition bannerPosition = AdViewPosition.BottomCenter;

        private Action callBackReward;


        public BannerAdManager(ApplovinMaxManager applovinMaxManager)
        {
            ApplovinMaxManager = applovinMaxManager;
        }
        void Awake()
        {

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
#if USE_APPSFLYER
                AppsFlyerManager.Instance.Init();
#endif
#if USE_ADJUST
            AdjustManager.Instance.Init();
#endif
                InitializeBannerAds();
            };

#if USE_APPSFLYER
            MaxSdk.SetUserId(AppsFlyer.getAppsFlyerId());
#endif
            MaxSdk.InitializeSdk();
        }



        #region Banner

        private void InitializeBannerAds()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

            ApplovinMaxManager.Invoke(nameof(LoadBanner), 0.5f);
        }

        private void LoadBanner()
        {
            var adViewConfiguration = new AdViewConfiguration(bannerPosition);
            MaxSdk.SetBannerWidth(bannerId, MaxSdkUtils.IsTablet() ? 800 : 350);
            MaxSdk.CreateBanner(bannerId, adViewConfiguration);
            MaxSdk.SetBannerBackgroundColor(bannerId, Color.clear);
        }


        public void ShowBanner()
        {
            if (RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Banner"))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_banner_ads", "");
                MaxSdk.ShowBanner(bannerId);
            }
        }

        public void HideBanner()
        {
            MaxSdk.HideBanner(bannerId);
        }

        private void OnBannerAdLoadedEvent(string adUnitId, AdInfo adInfo)
        {
            LogW.Log($"Ad Loaded", "Banner");
        }

        private void OnBannerAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
        {
            LogW.Log($"Ad Load Failed", "Banner");
        }

        private void OnBannerAdClickedEvent(string adUnitId, AdInfo adInfo)
        {
            FirebaseAnalyticManager.Instance.LogAd("show_banner_ads_click", "");
        }

        private void OnBannerAdExpandedEvent(string adUnitId, AdInfo adInfo) { }

        private void OnBannerAdCollapsedEvent(string adUnitId, AdInfo adInfo) { }

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
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

    public class BannerModule
    {
        ApplovinMaxManager ApplovinMamager;
        string bannerId = "banner-id";


        public BannerModule(ApplovinMaxManager applovin, string bannerId)
        {
            ApplovinMamager = applovin;
            this.bannerId = bannerId;
        }

        #region Banner

        public void Initialize()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

            ApplovinMamager.StartCoroutine(IELoadDelay(0.5f));
        }

        private IEnumerator IELoadDelay(float time)
        {
            yield return new WaitForSeconds(time);
            LoadBanner();
        }

        private void LoadBanner()
        {
            var adViewConfiguration = new AdViewConfiguration(ApplovinMamager.AdConstants.bannerPosition);
            // MaxSdk.SetBannerWidth(bannerId, MaxSdkUtils.IsTablet() ? 800 : 350);
            MaxSdk.CreateBanner(bannerId, adViewConfiguration);
            MaxSdk.SetBannerBackgroundColor(bannerId, Color.black);
        }


        public void ShowBanner()
        {
            if (RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowBanner)
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
            LogMessage.Log($"Ad Loaded", "Banner");
        }

        private void OnBannerAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
        {
            LogMessage.Log($"Ad Load Failed: {errorInfo.Message}", "Banner");
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
            ApplovinMamager.SendAdRevenueTracker(impressionData);
        }
        #endregion
    }


}
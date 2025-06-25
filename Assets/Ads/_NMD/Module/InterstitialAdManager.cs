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

    public class InterstitialAdManager : MonoBehaviour
    {
        public ApplovinMaxManager ApplovinMaxManager;
        [SerializeField] string intersId = "interstitial-id";

        [Header("Interstitial")]
        private Action callBackInters;
        private int intersRetry;
        private string interLocation;

        public InterstitialAdManager(ApplovinMaxManager applovinMaxManager)
        {
            ApplovinMaxManager = applovinMaxManager;
        }

        #region Interstitial
        private void InitializeInterstitialAds()
        {
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += InterstitialOnAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdHiddenEvent;

            LoadInterstitial();
        }

        public void LoadInterstitial()
        {
            if (MaxSdk.IsInitialized())
            {
                if (!MaxSdk.IsInterstitialReady(intersId))
                {
                    MaxSdk.LoadInterstitial(intersId);
                }
            }
        }

        public void ShowInterstitial(string location, Action callBack)
        {
            if (!RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Interstitial"))
            {
                callBack?.Invoke();
                return;
            }

            interLocation = location;
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads", interLocation);
            isCanShowAOABackGame = false;
            bool passedCappingTime = Time.unscaledTime >= timeCloseAOA;
            if (MaxSdk.IsInterstitialReady(intersId) && passedCappingTime)
            {
                FirebaseAnalyticManager.Instance.LogAd("show_interstitial_success", interLocation);
                callBackInters = callBack;
                MaxSdk.ShowInterstitial(intersId);
            }
            else
            {
                LoadInterstitial();
                isCanShowAOABackGame = true;
                callBack?.Invoke();
            }
        }

        private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            intersRetry = 0;
            LogW.Log($"Ad Loaded", "Interstitial");
        }

        private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            intersRetry++;
            double retryDelay = Math.Pow(2, Math.Min(6, intersRetry));
            Invoke(nameof(LoadInterstitial), (float)retryDelay);
            LogW.Log($"Ad Load Failed", "Interstitial");
        }

        private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Displayed", "Interstitial");
        }

        private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (callBackInters != null)
            {
                callBackInters.Invoke();
                callBackInters = null;
            }
            isCanShowAOABackGame = true;
            Invoke(nameof(LoadInterstitial), 0.5f);
            LogW.Log($"Ad Display Failed", "Interstitial");
        }

        private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Click", "Interstitial");
            if (!string.IsNullOrEmpty(interLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_interstitial_click", interLocation);
            }
        }

        private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (callBackInters != null)
            {
                callBackInters.Invoke();
                callBackInters = null;
                timeCloseAOA = Time.unscaledTime;
            }
            isCanShowAOABackGame = true;
            Invoke(nameof(LoadInterstitial), 0.5f);
            LogW.Log($"Ad Hidden", "Interstitial");
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
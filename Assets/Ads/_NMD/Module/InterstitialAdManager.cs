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

    public class InterstitialAdManager
    {
        public ApplovinMaxManager ApplovinManager;
        private string intersId = "inters-id";
        public InterstitialAdManager(ApplovinMaxManager applovinMaxManager, string intersId)
        {
            ApplovinManager = applovinMaxManager;
            this.intersId = intersId;
        }

        #region Interstitial
        public void Initialize()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += InterstitialOnAdClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdHiddenEvent;

            ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
        }

        private IEnumerator IELoadDelay(float time)
        {
            yield return new WaitForSeconds(time);
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
            if (!RemoteConfigManager.GetConfigValue<bool>(RemoteKey.AdUseInterstitial))
            {
                callBack?.Invoke();
                return;
            }

            ApplovinManager.interLocation = location;
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads", location);
            ApplovinManager.isCanShowAOABackGame = false;
            bool passedCappingTime = Time.unscaledTime >= ApplovinManager.timeCloseAOA;
            if (MaxSdk.IsInterstitialReady(intersId) && passedCappingTime)
            {
                FirebaseAnalyticManager.Instance.LogAd("show_interstitial_success", location);
                ApplovinManager.callBackInters = callBack;
                MaxSdk.ShowInterstitial(intersId);
            }
            else
            {
                LoadInterstitial();
                ApplovinManager.isCanShowAOABackGame = true;
                callBack?.Invoke();
            }
        }

        private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            ApplovinManager.intersRetry = 0;
            LogW.Log($"Ad Loaded", "Interstitial");
        }

        private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            ApplovinManager.intersRetry++;
            double retryDelay = Math.Pow(2, Math.Min(6, ApplovinManager.intersRetry));
            ApplovinManager.StartCoroutine(IELoadDelay((float)retryDelay));
            LogW.Log($"Ad Load Failed", "Interstitial");
        }

        private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Displayed", "Interstitial");
        }

        private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (ApplovinManager.callBackInters != null)
            {
                ApplovinManager.callBackInters?.Invoke();
                ApplovinManager.callBackInters = null;
            }
            ApplovinManager.isCanShowAOABackGame = true;
            ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
            LogW.Log($"Ad Display Failed", "Interstitial");
        }

        private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Click", "Interstitial");
            if (!string.IsNullOrEmpty(ApplovinManager.interLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_interstitial_click", ApplovinManager.interLocation);
            }
        }

        private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (ApplovinManager.callBackInters != null)
            {
                ApplovinManager.callBackInters.Invoke();
                ApplovinManager.callBackInters = null;
                ApplovinManager.timeCloseAOA = Time.unscaledTime;
            }
            ApplovinManager.isCanShowAOABackGame = true;
            ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
            LogW.Log($"Ad Hidden", "Interstitial");
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
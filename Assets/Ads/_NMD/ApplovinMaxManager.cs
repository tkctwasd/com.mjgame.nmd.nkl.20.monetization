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
    using System;
    using UnityEditor;
    using UnityEngine;
    using static MaxSdkBase;

    public class ApplovinMaxManager : MonoBehaviour
    {
        [Header("References")]
        public static ApplovinMaxManager Instance = null;
        public AdConstants AdConstants;
        public AppOpenAdManager AppOpenAdManager;
        public BannerAdManager BannerAdManager;
        public InterstitialAdManager InterstitialAdManager;
        public RewardedAdManager RewardedAdManager;
        public AdRevenueTracker AdRevenueTracker;

        [Header("AppOpen")]
        internal float timeCloseAOA;
        internal string appOpenLocation;
        internal bool IsCloseFirstAOA;
        internal bool isCanShowAOABackGame;
        internal bool IsShowFirstAOA;
        internal bool isLogEventLoading;
        internal DateTime TimeCloseAOA { get; set; }

        [Header("Banner")]
        internal Action callBackReward;

        [Header("Interstitial")]
        internal Action callBackInters;
        internal int intersRetry;
        internal string interLocation;

        [Header("Rewarded")]
        internal int rewardedRetry;
        internal string rewardLocation;

        void Awake()
        {
            Instance = this;

            InterstitialAdManager = new InterstitialAdManager(this, AdConstants.InterstitialId);
            RewardedAdManager = new RewardedAdManager(this, AdConstants.RewardedId);
            BannerAdManager = new BannerAdManager(this, AdConstants.BannerId);
            AppOpenAdManager = new AppOpenAdManager(this, AdConstants.BannerId);
            AdRevenueTracker = new AdRevenueTracker();

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                Debug.Log("MAX SDK Initialized");
#if USE_APPSFLYER
                AppsFlyerManager.Instance.Init();
#endif
#if USE_ADJUST
            AdjustManager.Instance.Init();
#endif
                AppOpenAdManager.Initialize();
                InterstitialAdManager.Initialize();
                BannerAdManager.Initialize();
                RewardedAdManager.Initialize();
            };

#if USE_APPSFLYER
            MaxSdk.SetUserId(AppsFlyer.getAppsFlyerId());
#endif
            MaxSdk.InitializeSdk();
        }

        private void Start()
        {
            RemoteConfigManager.FetchDone += LogKeyAd;
        }

        [ContextMenu("Set ID")]
        public void SetId()
        {
#if UNITY_EDITOR
            AppLovinSettings.Instance.SdkKey = AdConstants.SdkKey;
            AppLovinInternalSettings.Instance.ConsentFlowEnabled = true;
            AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = AdConstants.PrivacyPolicyURL;
            AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = AdConstants.TermsURL;
            PlayerSettings.productName = AdConstants.GameName;
            PlayerSettings.applicationIdentifier = AdConstants.PackageName;
#if UNITY_ANDROID
            AppLovinSettings.Instance.AdMobAndroidAppId = AdConstants.AdmobAndroidAppId;
#elif UNITY_IOS
            AppLovinSettings.Instance.AdMobIosAppId = AdConstants.AdmobIosAppId;
#endif
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            Debug.Log("Set ID");
        }
        #region Log Key
        private void LogKeyAd()
        {
            if (RemoteConfigManager.GetConfigValue<bool>("Ad_Debug_Key"))
            {
                LogW.Log($"SDK MAX: {AdConstants.SdkKey}", "Key Ad");
                LogW.Log($"App Open ID: {AdConstants.AppOpenId}", "Key Ad");
                LogW.Log($"Banner ID: {AdConstants.BannerId}", "Key Ad");
                LogW.Log($"Interstitial ID: {AdConstants.InterstitialId}", "Key Ad");
                LogW.Log($"Rewarded: {AdConstants.RewardedId}", "Key Ad");
#if USE_APPSFLYER
                LogW.Log($"Appsflyer: {AppsFlyerManager.Instance.GetDevKeyAndAppId()}", "Key Ad");
#endif
#if USE_ADJUST
            LogW.Log($"Adjust: {AdjustManager.Instance.GetTokenAdjust()}", "Key Ad");
#endif
            }
        }
        #endregion

        public void SendAdRevenueTracker(AdInfo impressionData)
        {
            AdRevenueTracker.SendAllTracker(impressionData);
        }
    }


}
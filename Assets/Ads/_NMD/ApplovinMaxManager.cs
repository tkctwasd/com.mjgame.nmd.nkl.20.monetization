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
        public AppOpenModule AppOpenAdManager;
        public BannerModule BannerAdManager;
        public InterstitialModule InterstitialAdManager;
        public RewardedModule RewardedAdManager;
        public AdRevenueTracker AdRevenueTracker;

        [Header("AppOpen")]
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
        internal DateTime TimeCloseInterstitial { get; set; }

        [Header("Rewarded")]
        internal int rewardedRetry;
        internal string rewardLocation;

        void Awake()
        {
            Instance = this;

            InterstitialAdManager = new InterstitialModule(this, AdConstants.InterstitialId);
            RewardedAdManager = new RewardedModule(this, AdConstants.RewardedId);
            BannerAdManager = new BannerModule(this, AdConstants.BannerId);
            AppOpenAdManager = new AppOpenModule(this, AdConstants.AppOpenId);
            AdRevenueTracker = new AdRevenueTracker();

            TimeCloseAOA = DateTime.MinValue;
            TimeCloseInterstitial = DateTime.MinValue;

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

        private void OnEnable()
        {
            RemoteConfigManager.ActionShowInfo += LogKeyAd;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // if (!pauseStatus)
            // {
            //     Debug.Log("<color=yellow>Load AOA Pause</color>");
            //     if (AppOpenAdManager != null)
            //     {
            //         AppOpenAdManager.ShowAppOpen(false);
            //     }
            //     else
            //     {
            //         Debug.LogWarning("AppOpenAdManager chưa được khởi tạo khi resume!");
            //     }
            // }

            if (pauseStatus)
            {
                // Lưu lại trạng thái app đã từng vào nền
                IsCloseFirstAOA = true;
            }
            else
            {
                // Chỉ show AOA khi quay lại từ nền, không phải lần mở app đầu
                if (IsCloseFirstAOA && AppOpenAdManager != null && MaxSdk.IsInitialized())
                {
                    Debug.Log("<color=yellow>Load AOA Pause</color>");
                    AppOpenAdManager.ShowAppOpen(false);

                    // Reset nếu bạn không muốn lặp lại
                    IsCloseFirstAOA = false;
                }
            }
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
            if (RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).ShowInfo)
            {
                LogMessage.Log($"SDK MAX: {AdConstants.SdkKey}", "Key Ad");
                LogMessage.Log($"App Open ID: {AdConstants.AppOpenId}", "Key Ad");
                LogMessage.Log($"Banner ID: {AdConstants.BannerId}", "Key Ad");
                LogMessage.Log($"Interstitial ID: {AdConstants.InterstitialId}", "Key Ad");
                LogMessage.Log($"Rewarded: {AdConstants.RewardedId}", "Key Ad");
#if USE_APPSFLYER
                LogMessage.Log($"Appsflyer: {AppsFlyerManager.Instance.GetDevKeyAndAppId()}", "Key Ad");
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
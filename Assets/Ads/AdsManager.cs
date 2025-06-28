#if USE_ADJUST
using AdjustSdk;
using AppLovinMax.Scripts.IntegrationManager.Editor;

#endif
#if USE_APPSFLYER
using AppsFlyerSDK;
#endif
using System;
using UnityEngine;
using static MaxSdkBase;

public class AdsManager : MonoBehaviour
{
    [Header("References")]
    public static AdsManager Instance = null;
    public AdConstants AdConstants;
    public AppOpenModule AppOpenAdModule;
    public BannerModule BannerAdModule;
    public InterstitialModule InterstitialModule;
    public RewardedModule RewardedModule;
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

        InterstitialModule = new InterstitialModule(this, AdConstants.InterstitialId);
        RewardedModule = new RewardedModule(this, AdConstants.RewardedId);
        BannerAdModule = new BannerModule(this, AdConstants.BannerId);
        AppOpenAdModule = new AppOpenModule(this, AdConstants.AppOpenId);

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
            AppOpenAdModule.Initialize();
            InterstitialModule.Initialize();
            BannerAdModule.Initialize();
            RewardedModule.Initialize();
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
        if (pauseStatus)
        {
            IsCloseFirstAOA = true;
        }
        else
        {
            if (IsCloseFirstAOA && AppOpenAdModule != null && MaxSdk.IsInitialized())
            {
                LogMessage.Log("AOA Load Pause", "AdsManager");
                AppOpenAdModule.ShowAppOpen(false);
                IsCloseFirstAOA = false;
            }
        }
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
            LogMessage.Log($"Adjust: {AdjustManager.Instance.GetTokenAdjust()}", "Key Ad");
#endif
        }
    }
    #endregion

    public void SendAdRevenueTracker(AdInfo impressionData)
    {
        AdRevenueTracker.SendAllTracker(impressionData);
    }
}
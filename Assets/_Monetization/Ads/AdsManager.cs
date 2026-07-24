using System;
using UnityEngine;
using static MaxSdkBase;

public class AdsManager : MonoBehaviour
{
    [Header("References")]
    public static AdsManager Instance = null;
    public AdConstants AdConstants;
    public AppOpenModule AppOpenModule;
    public BannerModule BannerModule;
    public MRECModule MRECModule;
    public InterstitialModule InterstitialModule;
    public RewardedModule RewardedModule;
    public AdRevenueTracker AdRevenueTracker;

    [Header("Ads")]
    internal bool IsShowAds;
    internal DateTime TimeStart;

    [Header("AppOpen")]
    internal bool IsCloseFirstAOA;
    internal bool IsCanShowAOABackGame;
    internal bool IsShowFirstAOA;
    internal DateTime LastTimeShowAOA;
    internal Action callAOADismissed;
    public static event Action OnResumeShow;
    public static event Action OnResumeComplete;

    [Header("Interstitial")]
    internal DateTime LastTimeShowInterstitial;

    [Header("Rewarded")]
    internal DateTime LastTimeShowReward;

    [Header("MREC")]
    internal bool ActiveMREC = false;


    void Awake()
    {
        Instance = this;


        InterstitialModule = new InterstitialModule(this, AdConstants.InterstitialIdHigh, AdConstants.InterstitialIdMedium, AdConstants.InterstitialIdMain);
        RewardedModule = new RewardedModule(this, AdConstants.RewardedIdHigh, AdConstants.RewardedIdMedium, AdConstants.RewardedIdMain);
        BannerModule = new BannerModule(this, AdConstants.BannerId);
        MRECModule = new MRECModule(this, AdConstants.MRECId);
        AppOpenModule = new AppOpenModule(this, AdConstants.AppOpenId);

        AdRevenueTracker = new AdRevenueTracker();

        LastTimeShowAOA = DateTime.MinValue;
        LastTimeShowInterstitial = DateTime.MinValue;
        LastTimeShowReward = DateTime.MinValue;
        TimeStart = DateTime.UtcNow;

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            Debug.Log("MAX SDK Initialized");
#if USE_APPSFLYER
            AppsFlyerManager.Instance.Init();
#endif
#if USE_ADJUST
            AdjustManager.Instance.Init();
#endif
            // Bid Floor test: disable auto retries AFTER init (before init it crashes).
            // We drive the retry loop ourselves via the HIGH -> MEDIUM -> MAIN waterfall.
            DisableAutoRetries();

            AppOpenModule.Initialize();
            InterstitialModule.Initialize();
            BannerModule.Initialize();
            MRECModule.Initialize();
            RewardedModule.Initialize();
        };

#if USE_APPSFLYER
        MaxSdk.SetUserId(AppsFlyerSDK.AppsFlyer.getAppsFlyerId());
#endif
        // Bid Floor test: disable back-to-back BEFORE init so the manual waterfall controls loading.
        DisableBackToBack();
        MaxSdk.InitializeSdk();

        callAOADismissed += OnAOADismissed;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (!IsShowAds)
            {
                IsCloseFirstAOA = true;
            }
        }
        else
        {
            if (IsCloseFirstAOA && !IsShowAds)
            {
                bool canshowAOA = AppOpenModule.CanShowAOA(false);
                Debug.Log(canshowAOA);
                if (AppOpenModule != null && MaxSdk.IsInitialized() && canshowAOA)
                {
                    MRECModule.HideMREC(true);
                    BannerModule.HideBanner();
                    Debug.Log("AdsManager".Ads("Back to game from background - Show Resume"));
                    OnResumeShow?.Invoke();
                }
            }
            IsCloseFirstAOA = false;
            IsShowAds = false;
        }
    }

    public bool IsRemoveAdsPurchased => false;

    public void OnAOADismissed()
    {
        var adsConfig = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (ActiveMREC && adsConfig.ShowMREC)
        {
            Debug.Log("MREC".Ads("Resume after AOA Dismissed"));
            MRECModule.ResumeMREC();
            if (!adsConfig.SwitchBannerMREC)
            {
                Debug.Log("MREC-Banner".Ads("Keep Banner show because SwitchBannerMREC = false [Resume]"));
                BannerModule.ShowBanner();
            }
        }
        else
        {
            BannerModule.ShowBanner();
        }
        OnResumeComplete?.Invoke();
    }

    public void SendAdRevenueTracker(AdInfo impressionData)
    {
        AdRevenueTracker.SendAllTracker(impressionData);
    }

    // Bid Floor test setup ------------------------------------------------

    private void DisableBackToBack()
    {
        string ids = string.Join(",", new[]
        {
            AdConstants.InterstitialIdHigh, AdConstants.InterstitialIdMedium, AdConstants.InterstitialIdMain,
            AdConstants.RewardedIdHigh, AdConstants.RewardedIdMedium, AdConstants.RewardedIdMain
        });
        MaxSdk.SetExtraParameter("disable_b2b_ad_unit_ids", ids);
    }

    private void DisableAutoRetries()
    {
        MaxSdk.SetInterstitialExtraParameter(AdConstants.InterstitialIdHigh, "disable_auto_retries", "true");
        MaxSdk.SetInterstitialExtraParameter(AdConstants.InterstitialIdMedium, "disable_auto_retries", "true");
        MaxSdk.SetInterstitialExtraParameter(AdConstants.InterstitialIdMain, "disable_auto_retries", "true");

        MaxSdk.SetRewardedAdExtraParameter(AdConstants.RewardedIdHigh, "disable_auto_retries", "true");
        MaxSdk.SetRewardedAdExtraParameter(AdConstants.RewardedIdMedium, "disable_auto_retries", "true");
        MaxSdk.SetRewardedAdExtraParameter(AdConstants.RewardedIdMain, "disable_auto_retries", "true");
    }


}
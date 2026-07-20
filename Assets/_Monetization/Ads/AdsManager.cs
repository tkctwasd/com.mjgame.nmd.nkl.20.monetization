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

    [Header("Interstitial")]
    internal DateTime LastTimeShowInterstitial;

    [Header("Rewarded")]
    internal DateTime LastTimeShowReward;

    [Header("MREC")]
    internal bool ActiveMREC = false;

    void Awake()
    {
        Instance = this;


        InterstitialModule = new InterstitialModule(this, AdConstants.InterstitialId);
        RewardedModule = new RewardedModule(this, AdConstants.RewardedId);
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
            AppOpenModule.Initialize();
            InterstitialModule.Initialize();
            BannerModule.Initialize();
            MRECModule.Initialize();
            RewardedModule.Initialize();
        };

#if USE_APPSFLYER
        MaxSdk.SetUserId(AppsFlyerSDK.AppsFlyer.getAppsFlyerId());
#endif
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
                bool canshowAOA = AppOpenModule.CanShowAOA(true);
                Debug.Log(canshowAOA);
                if (AppOpenModule != null && MaxSdk.IsInitialized() && canshowAOA)
                {
                    MRECModule.HideMREC(true);
                    BannerModule.HideBanner();
                    Debug.Log("AdsManager".Ads("Back to game from background - Show Resume"));
                    UIManager.Instance.ShowPanel<PanelResume>();
                }
            }
            IsCloseFirstAOA = false;
            IsShowAds = false;
        }
    }

    public bool IsRemoveAdsPurchased => IAPManager.Instance?.PurchaseModule?.IsRemoveAdsPurchased() == true || DataSave.Instance?.DataMonetization?.IsRemoveAdsPurchased == true;

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
        UIManager.Instance.GetPanel<PanelResume>().Complete();
    }

    public void SendAdRevenueTracker(AdInfo impressionData)
    {
        AdRevenueTracker.SendAllTracker(impressionData);
    }


}
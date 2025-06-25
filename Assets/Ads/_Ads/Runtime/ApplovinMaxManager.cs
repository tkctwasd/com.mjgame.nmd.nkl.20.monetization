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

public class ApplovinMaxManager : MonoBehaviour
{
    public static ApplovinMaxManager ins = null;
    [SerializeField] string nameGame = "name-game";
    [SerializeField] string packageName = "id-package-name";
    [SerializeField] string sdkKey = "sdk-key";
    [SerializeField] string admobId = "admob-id";
    [SerializeField] string privacyPolicyURL = "privacy-policy-URL";
    [SerializeField] string termsURL = "terms-URL";
    [Header("Ad ID")]
    [SerializeField] string appOpenId = "app-open-id";
    [SerializeField] string bannerId = "banner-id";
    [SerializeField] string intersId = "interstitial-id";
    [SerializeField] string rewardId = "rewarded-id";

    [Header("AppOpen")]
    private float timeCloseAOA;
    private string appOpenLocation;

    internal bool IsCloseFirstAOA;
    private bool isCanShowAOABackGame;
    internal bool IsShowFirstAOA;
    private bool isLogEventLoading;
    public DateTime TimeCloseAOA { get; set; }

    [Header("Banner")]
    [SerializeField] private AdViewPosition bannerPosition = AdViewPosition.BottomCenter;

    private Action callBackReward;

    [Header("Interstitial")]
    private Action callBackInters;
    private int intersRetry;
    private string interLocation;

    [Header("Rewarded")]
    private int rewardedRetry;

    private string rewardLocation;

    void Awake()
    {
        ins = this;

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            Debug.Log("MAX SDK Initialized");
#if USE_APPSFLYER
            AppsFlyerManager.Instance.Init();
#endif
#if USE_ADJUST
            AdjustManager.Instance.Init();
#endif
            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeBannerAds();
            InitializeAppOpen();
        };

#if USE_APPSFLYER
        MaxSdk.SetUserId(AppsFlyer.getAppsFlyerId());
#endif
        MaxSdk.InitializeSdk();
    }

    private void Start()
    {
        timeCloseAOA = -1000f;
        isCanShowAOABackGame = true;
        RemoteConfigManager.FetchDone += LogKeyAd;
    }
    [ContextMenu("Set ID")]
    public void SetId()
    {
#if UNITY_EDITOR
        AppLovinSettings.Instance.SdkKey = sdkKey;
        AppLovinInternalSettings.Instance.ConsentFlowEnabled = true;
        AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = privacyPolicyURL;
        AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = termsURL;
        PlayerSettings.productName = nameGame;
        PlayerSettings.applicationIdentifier = packageName;
#if UNITY_ANDROID
        AppLovinSettings.Instance.AdMobAndroidAppId = admobId;

#elif UNITY_IOS
        AppLovinSettings.Instance.AdMobIosAppId = admobId;
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
        Debug.Log("Set ID");
    }
    #region Log Key
    private void LogKeyAd()
    {
        if(RemoteConfigManager.GetConfigValue<bool>("Ad_Debug_Key"))
        {
            LogW.Log($"SDK MAX: {sdkKey}", "Key Ad");
            LogW.Log($"App Open ID: {appOpenId}", "Key Ad");
            LogW.Log($"Banner ID: {bannerId}", "Key Ad");
            LogW.Log($"Interstitial ID: {intersId}", "Key Ad");
            LogW.Log($"Rewarded: {rewardId}", "Key Ad");
#if USE_APPSFLYER
            LogW.Log($"Appsflyer: {AppsFlyerManager.Instance.GetDevKeyAndAppId()}", "Key Ad");
#endif
#if USE_ADJUST
            LogW.Log($"Adjust: {AdjustManager.Instance.GetTokenAdjust()}", "Key Ad");
#endif
        }
    }
    #endregion

    #region App Open 
    private void OnApplicationPause(bool pauseStatus)
    {
        if(!pauseStatus)
        {
            ShowAppOpen(false);
        }
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
        if(!IsShowFirstAOA) IsShowFirstAOA = true;
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
        if(!IsCloseFirstAOA) IsCloseFirstAOA = true;
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
        if(!string.IsNullOrEmpty(appOpenLocation))
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
        if((isLoading || passedCappingTime) && IsShowAOA())
        {
            if((!isLogEventLoading && isLoading) || !isLoading)
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads", appOpenLocation);
                isLogEventLoading = true;
            }
            if(MaxSdk.IsAppOpenAdReady(appOpenId))
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

    #region Banner

    private void InitializeBannerAds()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

        Invoke(nameof(LoadBanner), 0.5f);
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
        if(RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Banner"))
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
        if(MaxSdk.IsInitialized())
        {
            if(!MaxSdk.IsInterstitialReady(intersId))
            {
                MaxSdk.LoadInterstitial(intersId);
            }
        }
    }

    public void ShowInterstitial(string location, Action callBack)
    {
        if(!RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Interstitial"))
        {
            callBack?.Invoke();
            return;
        }

        interLocation = location;
        FirebaseAnalyticManager.Instance.LogAd("show_interstitial_ads", interLocation);
        isCanShowAOABackGame = false;
        bool passedCappingTime = Time.unscaledTime >= timeCloseAOA;
        if(MaxSdk.IsInterstitialReady(intersId) && passedCappingTime)
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
        if(callBackInters != null)
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
        if(!string.IsNullOrEmpty(interLocation))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_interstitial_click", interLocation);
        }
    }

    private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if(callBackInters != null)
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

    #region Rewarded

    private void InitializeRewardedAds()
    {
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedOnAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedOnAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedOnAdDisplayFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedOnAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedOnAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedOnAdReceivedRewardEvent;

        Invoke(nameof(LoadRewardedAd), 0.5f);
    }

    private void LoadRewardedAd()
    {
        if(MaxSdk.IsInitialized())
        {
            MaxSdk.LoadRewardedAd(rewardId);
        }
    }

    public void ShowRewarded(string location, Action callBack)
    {
        rewardLocation = location;
        isCanShowAOABackGame = false;
        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads", rewardLocation);
        if(MaxSdk.IsRewardedAdReady(rewardId))
        {
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_sucess", rewardLocation);
            callBackReward = callBack;
            MaxSdk.ShowRewardedAd(rewardId);
        }
    }

    private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log($"Ad Loaded", "Rewarded");
        rewardedRetry = 0;
    }

    private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        rewardedRetry++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetry));
        Invoke(nameof(LoadRewardedAd), (float)retryDelay);
        LogW.Log($"Ad Load Failed + {errorInfo.Message}", "Rewarded");
    }

    private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        Invoke(nameof(LoadRewardedAd), 0.5f);
        isCanShowAOABackGame = true;
        LogW.Log($"Ad Display Failed + {errorInfo.Message}", "Rewarded");
    }

    private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log("Ad Displayed", "Rewarded");
    }

    private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log("Ad Clicked", "Rewarded");
        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_click", rewardLocation);
    }

    private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogW.Log("Ad Hidden", "Rewarded");
        timeCloseAOA = Time.unscaledTime;
        Invoke(nameof(LoadRewardedAd), 0.5f);
        isCanShowAOABackGame = true;
    }

    private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        if(callBackReward != null)
        {
            callBackReward.Invoke();
            callBackReward = null;
        }
        isCanShowAOABackGame = true;
        FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_completed", rewardLocation);
        LogW.Log("Ad Completed", "Rewarded");
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


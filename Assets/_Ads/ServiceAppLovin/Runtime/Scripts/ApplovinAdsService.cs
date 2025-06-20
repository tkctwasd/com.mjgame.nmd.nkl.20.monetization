using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class AppLovinAdsService : IAdsService
{
    public AppLovinConfig Config { get; private set; }
    public bool IsAppOpenClosed { get; private set; }
    public bool IsAutoLoadInterstitial;
    string placementInterstitial { get; set; }
    string placementReward { get; set; }
    bool isReady { get; set; }
    [RuntimeInitializeOnLoadMethod]
    static void InitAppLovin()
    {
        var ads = new AppLovinAdsService();
        ServiceAds.Register(ads);
        ads.Initialize();
        Wasd.LogStateInitialize("AppLovinAds");
    }
    public void Initialize()
    {
        Task.Delay(100);
        var data = Resources.Load<TextAsset>("Info");
        JObject jObject = JObject.Parse(data.text);
        Config = new AppLovinConfig();
        Config.AppOpenId = jObject["AppOpen"].ToString();
        Config.BannerId = jObject["Banner"].ToString();
        Config.InterstitialId = jObject["Interstitial"].ToString();
        Config.RewardedId = jObject["Rewarded"].ToString();
        Config.SdkKey = jObject["SDKMax"].ToString();
        if(jObject.ContainsKey("BannerPosition"))
        {
            API.Get<ServiceAds>().Setting.BannerPosition = (BannerPosition)Enum.Parse(typeof(BannerPosition), jObject["BannerPosition"].ToString());
        }
        countInterstitialShowed = PlayerPrefs.GetInt("InterstitialShowed", 0);
        countRewardedShowed = PlayerPrefs.GetInt("RewardedShowed", 0);
        IsAutoLoadInterstitial = true;
        LoadInfo();
    }

    public async void LoadInfo()
    {
        await Task.Delay(500);
        if(Debug.isDebugBuild)
        {
            Wasd.Log("App Open Id: " + Config.AppOpenId);
            Wasd.Log("Banner Id: " + Config.BannerId);
            Wasd.Log("Interstitial Id: " + Config.InterstitialId);
            Wasd.Log("Sdk Max: " + Config.SdkKey);
        }
        else
        {
            Wasd.Log("Initialize Ads " + Config.AppOpenId + " " + Config.BannerId + " " + Config.InterstitialId + " " + Config.RewardedId);
        }

        if(!string.IsNullOrEmpty(Config.SdkKey))
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += async sdkConfiguration =>
            {
#if UNITY_IOS
                ServiceAds.SetAdvertiserTrackingEnabled(sdkConfiguration.AppTrackingStatus == MaxSdkBase.AppTrackingStatus.Authorized);
#endif
                RequestRewarded();
                RequestAppOpen();
                RequestBanner();
                RequestInterstitial();
                isReady = true;
                Wasd.Log("Tracking set info");
                API.Get<ServiceTracking>().SetInfo();
                await API.Get<ServiceRemoteConfig>().FetchRemoteConfig();
                API.Get<ServiceAds>().SetReady();
            };
            ServiceAds.SetDataProcessingOptions(new string[] { "LDU" }, 0, 0);
            MaxSdk.InitializeSdk();
        }
        else
        {
            Wasd.LogError("SDKKey is null");
        }
    }

    public AdRevenueInfo GetRevenueInfo(MaxSdk.AdInfo adInfo)
    {
        AdRevenueInfo revenueInfo = new AdRevenueInfo()
        {
            AdNetwork = adInfo.NetworkName,
            AdUnit = adInfo.AdUnitIdentifier,
            AdFormat = adInfo.AdFormat,
            AdPlacement = adInfo.Placement,
            AdRevenue = adInfo.Revenue,
            CountryCode = MaxSdk.GetSdkConfiguration().CountryCode,
            AdCurrency = "USD"
        };
        return revenueInfo;
    }
    public bool CanShowAds()
    {
        return API.Get<ServiceAds>().CanShowAds();
    }

    #region APP_OPEN
    bool isRequestAppOpen { get; set; }
    string placementAppOpen { get; set; }
    UnityAction CallbackAppOpen { get; set; } = null;
    void RequestAppOpen()
    {
        if(!CanShowAds())
        {
            return;
        }
        if(Debug.isDebugBuild)
        {
            Wasd.Log("Request", "AppOpen");
        }
        if(!API.Get<ServiceAds>().Setting.IsUseAppOpenAd) return;
        placementAppOpen = "";
        isRequestAppOpen = true;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenDisplayedEvent;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailedEvent;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenHiddenEvent;
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoadedEvent;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenLoadFailedEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenRevenuePaidEvent;
        MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenClickEvent;
        MaxSdk.LoadAppOpenAd(Config.AppOpenId);
        Wasd.Log("Request", "AppOpen");
    }
    private void OnAppOpenDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Show", "AppOpen");
        IsAppOpenClosed = false;
    }

    private void OnAppOpenLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if(Debug.isDebugBuild)
        {
            Wasd.Log("Load Success", "AppOpen");
        }
    }

    private void OnAppOpenLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Wasd.Log("Load Failed: " + errorInfo.Message, "AppOpen");
        MaxSdk.LoadAppOpenAd(Config.AppOpenId);
    }

    private void OnAppOpenDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Show Failed: " + errorInfo.Message, "AppOpen");
        MaxSdk.LoadAppOpenAd(Config.AppOpenId);
    }

    private void OnAppOpenRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceTracking>().OnAdsRevenue(GetRevenueInfo(adInfo));
    }

    private void OnAppOpenHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        IsAppOpenClosed = true;
        Wasd.Log("Hide", "AppOpen");
        placementAppOpen = "";
        MaxSdk.LoadAppOpenAd(Config.AppOpenId);
        API.Get<ServiceAds>().ShowFirstOpenDone = true;
        CallbackAppOpen?.Invoke();
        CallbackAppOpen = null;
    }

    private void OnAppOpenClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Clicked", "AppOpen");
        if(!string.IsNullOrEmpty(placementAppOpen))
        {
            API.Get<ServiceLogEvent>().OnAppOpenClick(placementAppOpen);
        }
    }

    public bool CanShowAppOpen()
    {
        if(!CanShowAds()) return false;
        if(API.Get<ServiceAds>().Setting.IsUseAppOpenAd == false) return false;
        if(API.Get<ServiceAds>().IsAdsLeaveApp) return false;
        return MaxSdk.IsAppOpenAdReady(Config.AppOpenId);
    }

    public bool IsAppOpenReady()
    {
        return CanShowAppOpen();
    }
    public void ShowAppOpen(string location, UnityAction callback)
    {
        if(!CanShowAds()) return;
        if(API.Get<ServiceAds>().Setting.IsUseAppOpenAd == false) return;
        if(API.Get<ServiceAds>().IsAdsLeaveApp)
        {
            API.Get<ServiceAds>().IsAdsLeaveApp = false;
            return;
        }
        placementAppOpen = location;
        CallbackAppOpen = callback;

        Wasd.Log($"Show In: {location}", "AppOpen");
        API.Get<ServiceLogEvent>().OnShowAppOpen(location);
        if(MaxSdk.IsAppOpenAdReady(Config.AppOpenId))
        {
            API.Get<ServiceAds>().IsAdsLeaveApp = true;
            MaxSdk.ShowAppOpenAd(Config.AppOpenId);
            API.Get<ServiceLogEvent>().OnShowAppOpenSuccess(location);
        }
        else
        {
            CallbackAppOpen?.Invoke();
            CallbackAppOpen = null;
        }
    }

    #endregion

    #region BANNER
    void RequestBanner()
    {
        if(!CanShowAds()) return;
        if(!API.Get<ServiceAds>().Setting.IsShowBanner) return;
        if(!API.Get<ServiceAds>().CanRequestBanner()) return;

        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
        if(MaxSdkUtils.IsTablet())
        {
            MaxSdk.SetBannerWidth(Config.BannerId, 800f);
        }
        else
        {
            MaxSdk.SetBannerWidth(Config.BannerId, 350f);
        }
        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
        var bannerPosition = API.Get<ServiceAds>().Setting.BannerPosition;
        var bannerColor = API.Get<ServiceAds>().Setting.BannerColor;
        MaxSdk.CreateBanner(Config.BannerId, (MaxSdkBase.BannerPosition)((int)bannerPosition));
        Color bannerBgColor;
        switch(bannerColor)
        {
            case BannerColor.NoColor:
                bannerBgColor = new Color(1f, 1f, 1f, 0f);
                break;
            default:
                bannerBgColor = Color.black;
                break;
        }
        // Set background or background color for banners to be fully functional.
        //MaxSdk.SetBannerBackgroundColor(Config.BannerId, bannerBgColor);       
        Wasd.Log("Request", "Banner");
    }
    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceTracking>().OnAdsRevenue(GetRevenueInfo(adInfo));
    }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceLogEvent>().OnBannerClick();
        if(!API.Get<ServiceAds>().CanRequestBanner())
        {
            HideBanner();
        }
        Wasd.Log("Clicked", "Banner");
    }
    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Loaded " + adInfo.NetworkName, "Banner");
    }

    private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Wasd.Log("Failed: " + errorInfo.Message, "Banner");
    }

    public void ShowBanner()
    {
        if(!CanShowAds()) return;
        if(!API.Get<ServiceAds>().CanRequestBanner()) return;
        if(!API.Get<ServiceAds>().Setting.IsShowBanner) return;
        API.Get<ServiceLogEvent>().OnShowBanner();
        Wasd.Log("Show", "Banner");
        MaxSdk.ShowBanner(Config.BannerId);
    }
    public void HideBanner()
    {
        Wasd.Log("Hide", "Banner");
        MaxSdk.HideBanner(Config.BannerId);
    }
    #endregion

    #region INTERSTITIAL
    int countInterstitialShowed { get; set; }
    float lastTimeShowInterstitial { get; set; }
    UnityAction CallbackInterstitial { get; set; } = null;
    float countRetryLoadInterstitial { get; set; }
    void RequestInterstitial()
    {
        if(!CanShowAds()) return;
        if(MaxSdk.IsInterstitialReady(Config.InterstitialId))
        {
            return;
        }
        placementInterstitial = "";
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
        MaxSdk.LoadInterstitial(Config.InterstitialId);
        Wasd.Log("Request", "Interstitial");
    }
    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Displayed", "Interstitial");
    }
    private void OnInterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        if(IsAutoLoadInterstitial)
            MaxSdk.LoadInterstitial(Config.InterstitialId);
        CallbackInterstitial?.Invoke();
        CallbackInterstitial = null;
        Wasd.Log("Failed To Display: " + errorInfo.Message, "Interstitial");
    }
    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        countRetryLoadInterstitial = 0;
        Wasd.Log("Loaded ", "Interstitial");
    }
    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceTracking>().OnAdsRevenue(GetRevenueInfo(adInfo));
    }
    private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        placementInterstitial = "";
        if(IsAutoLoadInterstitial)
        {
            MaxSdk.LoadInterstitial(Config.InterstitialId);
        }
        CallbackInterstitial?.Invoke();
        CallbackInterstitial = null;
        if(countInterstitialShowed < 3)
        {
            countInterstitialShowed++;
            PlayerPrefs.SetInt("InterstitialShowed", countInterstitialShowed);
            if(countInterstitialShowed == 3)
            {
                API.Get<ServiceLogEvent>().OnInterstitialNumReach(countInterstitialShowed);
            }
        }
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceAds>().IsAdsLeaveApp = true;
        if(!string.IsNullOrEmpty(placementInterstitial))
        {
            API.Get<ServiceLogEvent>().OnInterstitialClick(placementInterstitial);
        }
        Wasd.Log("Clicked", "Interstitial");

    }

    private async void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Wasd.Log("Load Failed: " + errorInfo.Message, "Interstitial");
        countRetryLoadInterstitial++;
        float retryDelay = Mathf.Pow(2, Math.Min(6, countRetryLoadInterstitial));
        await Task.Delay(TimeSpan.FromSeconds(retryDelay));
        MaxSdk.LoadInterstitial(Config.InterstitialId);
    }
    public bool CanShowInterstitial()
    {
        if(!isReady) return false;
        if(!CanShowAds()) return false;
        return MaxSdk.IsInterstitialReady(Config.InterstitialId);
    }
    public void ShowInterstitial(string location, UnityAction callback = null)
    {
        CallbackInterstitial = callback;
        placementInterstitial = location;
        API.Get<ServiceLogEvent>().OnShowInterstitial(MaxSdk.IsInterstitialReady(Config.InterstitialId), location);
        if(API.Get<ServiceAds>().CanShowInterstitial())
        {
            MaxSdk.ShowInterstitial(Config.InterstitialId);
            API.Get<ServiceLogEvent>().OnShowInterstitialSuccess(location);
            API.Get<ServiceAds>().OnShowInterstitial();
        }
        else
        {
            CallbackInterstitial?.Invoke();
            CallbackInterstitial = null;
        }
        Wasd.Log($"Show In: {location}", "Interstitial");

    }

    #endregion

    #region REWARDED
    int maxRetryRewarded => 3;
    int countRetryRewarded { get; set; }
    int countRewardedShowed { get; set; }

    bool triggerRewaredCallback { get; set; }
    UnityAction<bool> CallbackRewarded { get; set; } = null;

    void RequestRewarded()
    {
        if(!API.Get<ServiceAds>().CanRequestVideo())
        {
            return;
        }
        if(MaxSdk.IsRewardedAdReady(Config.RewardedId))
        {
            return;
        }
        placementReward = "";
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

        MaxSdk.LoadRewardedAd(Config.RewardedId);
        Wasd.Log("Request", "Rewarded");
    }
    #region Rewarded Callbacks
    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        API.Get<ServiceTracking>().OnAdsRevenue(GetRevenueInfo(adInfo));
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Show", "Rewarded");
        countRewardedShowed++;
    }

    public void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        placementReward = "";
        MaxSdk.LoadRewardedAd(Config.RewardedId);
        if(triggerRewaredCallback)
        {
            CallbackRewarded?.Invoke(false);
            CallbackRewarded = null;
        }
    }
    public void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        triggerRewaredCallback = false;
        MaxSdk.LoadRewardedAd(Config.RewardedId);
        CallbackRewarded?.Invoke(true);
        CallbackRewarded = null;

        if(countRewardedShowed < 3)
        {
            countRewardedShowed++;
            PlayerPrefs.SetInt("RewardedShowed", countRewardedShowed);
            if(countRewardedShowed == 3)
            {
                API.Get<ServiceLogEvent>().OnRewardedNumReach(countRewardedShowed);
            }
        }
    }
    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Wasd.Log("Clicked", "Rewarded");
        API.Get<ServiceAds>().IsAdsLeaveApp = true;
        API.Get<ServiceAds>().OnRewardedAdClicked(placementReward);
    }

    public void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        placementReward = "";
        Wasd.Log("Failed To Load: " + errorInfo.Message, "Rewarded");
        if(countRetryRewarded < maxRetryRewarded)
        {
            countRetryRewarded++;
            MaxSdk.LoadRewardedAd(Config.RewardedId);
        }
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. We recommend loading the next ad
        Wasd.Log("Failed To Display: " + errorInfo.Message, "Rewarded");
        MaxSdk.LoadRewardedAd(Config.RewardedId);
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        countRetryRewarded = 0;
        Wasd.Log("Loaded", "Rewarded");
    }
    #endregion
    public bool CanShowRewarded()
    {
        if(!CanShowAds()) return false;
        return (MaxSdk.IsRewardedAdReady(Config.RewardedId));
    }
    public void ShowRewarded(string location, UnityAction<bool> callback)
    {
        Wasd.Log("Show In: " + location, "Rewarded");
        bool isReady = MaxSdk.IsRewardedAdReady(Config.RewardedId);
        API.Get<ServiceLogEvent>().OnShowRewarded(isReady, location);
        if(isReady == false)
        {
            MaxSdk.LoadRewardedAd(Config.RewardedId);
        }
        else
        {
            triggerRewaredCallback = true;
            CallbackRewarded = callback;
            API.Get<ServiceAds>().IsAdsLeaveApp = true;
            placementReward = location;
            MaxSdk.ShowRewardedAd(Config.RewardedId);
            API.Get<ServiceLogEvent>().OnShowRewardedAdsSuccess(location);
        }
    }

    #endregion
}

[Serializable]
public class AppLovinConfig : AdsConfig
{
    public string SdkKey;
}
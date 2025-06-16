using System;
using UnityEngine;

public class MaxAdsManager : MonoBehaviour
{
    [Header("Ad Unit IDs")]
    public string bannerAdUnitId = "YOUR_BANNER_ID";
    public string interstitialAdUnitId = "YOUR_INTERSTITIAL_ID";
    public string rewardedAdUnitId = "YOUR_REWARDED_ID";
    public string appOpenAdUnitId = "YOUR_APP_OPEN_ID";

    [Header("SDK Settings")]
    public string sdkKey = "YOUR_SDK_KEY";

    [Header("Ad Settings")]
    public bool showBannerOnStart = true;
    public bool enableAppOpenAds = true;
    public float appOpenAdCooldown = 60f; // seconds between app open ads
    public MaxSdkBase.AdViewPosition bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;

    // Events
    public static event Action OnInterstitialClosed;
    public static event Action<bool> OnRewardedAdComplete; // true if rewarded

    private bool isInterstitialReady = false;
    private bool isRewardedReady = false;
    private bool isAppOpenAdReady = false;
    private float lastAppOpenAdTime = 0f;
    private bool isShowingAppOpenAd = false;

    #region Unity Lifecycle

    private void Start()
    {
        // Initialize MAX SDK
        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitializedEvent;
        MaxSdk.InitializeSdk();

        // Setup app lifecycle events for AOA
        if (enableAppOpenAds)
        {
            SetupAppLifecycleEvents();
        }
    }

    private void OnSdkInitializedEvent(MaxSdkBase.SdkConfiguration config)
    {
        Debug.Log("MAX SDK initialized successfully");
        // Initialize ads after SDK init
        InitializeBannerAds();
        InitializeInterstitialAds();
        InitializeRewardedAds();

        if (enableAppOpenAds)
        {
            InitializeAppOpenAds();
        }

        if (showBannerOnStart)
        {
            ShowBanner();
        }
    }

    #endregion

    #region App Open Ads

    private void SetupAppLifecycleEvents()
    {
        // Listen for app focus events
        Application.focusChanged += OnApplicationFocus;
        Application.runInBackground = true;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !isShowingAppOpenAd)
        {
            // App gained focus - show app open ad if conditions are met
            ShowAppOpenAdIfAvailable();
        }
    }

    private void InitializeAppOpenAds()
    {
        // App open ad callbacks
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
        MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickedEvent;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdFailedToDisplayEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaidEvent;

        // Load the first app open ad
        LoadAppOpenAd();
    }

    private void LoadAppOpenAd()
    {
        MaxSdk.LoadAppOpenAd(appOpenAdUnitId);
    }

    private void ShowAppOpenAdIfAvailable()
    {
        // Check if enough time has passed since last app open ad
        if (Time.realtimeSinceStartup - lastAppOpenAdTime < appOpenAdCooldown)
        {
            Debug.Log("App open ad cooldown active");
            return;
        }

        // Don't show if other fullscreen ads are showing
        if (isShowingAppOpenAd)
        {
            Debug.Log("App open ad already showing");
            return;
        }

        if (isAppOpenAdReady)
        {
            MaxSdk.ShowAppOpenAd(appOpenAdUnitId);
        }
        else
        {
            Debug.Log("App open ad not ready");
        }
    }

    public void ShowAppOpenAd()
    {
        if (isAppOpenAdReady)
        {
            MaxSdk.ShowAppOpenAd(appOpenAdUnitId);
        }
        else
        {
            Debug.Log("App open ad not ready");
        }
    }

    public bool IsAppOpenAdReady()
    {
        return isAppOpenAdReady;
    }

    private void OnAppOpenAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenAdReady = true;
        Debug.Log("App open ad loaded: " + adInfo.NetworkName);
    }

    private void OnAppOpenAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        isAppOpenAdReady = false;
        Debug.LogError("App open ad failed to load: " + errorInfo.Message);

        // Retry loading after 3 seconds
        Invoke(nameof(LoadAppOpenAd), 3f);
    }

    private void OnAppOpenAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isShowingAppOpenAd = true;
        Debug.Log("App open ad displayed");
    }

    private void OnAppOpenAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenAdReady = false;
        isShowingAppOpenAd = false;
        Debug.LogError("App open ad failed to display: " + errorInfo.Message);
        LoadAppOpenAd();
    }

    private void OnAppOpenAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("App open ad clicked");
    }

    private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isAppOpenAdReady = false;
        isShowingAppOpenAd = false;
        lastAppOpenAdTime = Time.realtimeSinceStartup;
        Debug.Log("App open ad hidden");

        // Load next app open ad
        LoadAppOpenAd();
    }

    private void OnAppOpenAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"App open ad revenue: ${adInfo.Revenue} from {adInfo.NetworkName}");
    }

    #endregion

    #region Banner Ads

    private void InitializeBannerAds()
    {
        // Banner callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

        // Create banner using AdViewConfiguration (recommended API)
        var bannerConfig = new MaxSdkBase.AdViewConfiguration(bannerPosition);
        MaxSdk.CreateBanner(bannerAdUnitId, bannerConfig);
        MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, Color.black);
    }

    public void ShowBanner()
    {
        MaxSdk.ShowBanner(bannerAdUnitId);
        isBannerVisible = true;
        Debug.Log("Banner ad shown");
    }

    public void HideBanner()
    {
        MaxSdk.HideBanner(bannerAdUnitId);
        isBannerVisible = false;
        Debug.Log("Banner ad hidden");
    }

    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Banner ad loaded: " + adInfo.NetworkName);
    }

    private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        Debug.LogError("Banner ad failed to load: " + errorInfo.Message);
    }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Banner ad clicked");
    }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"Banner revenue: ${adInfo.Revenue} from {adInfo.NetworkName}");
    }

    #endregion

    #region Interstitial Ads

    private void InitializeInterstitialAds()
    {
        // Interstitial callbacks
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;

        // Load the first interstitial
        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        MaxSdk.LoadInterstitial(interstitialAdUnitId);
    }

    public void ShowInterstitial()
    {
        if (isInterstitialReady)
        {
            MaxSdk.ShowInterstitial(interstitialAdUnitId);
        }
        else
        {
            Debug.Log("Interstitial ad not ready");
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isInterstitialReady = true;
        Debug.Log("Interstitial loaded: " + adInfo.NetworkName);
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        isInterstitialReady = false;
        Debug.LogError("Interstitial failed to load: " + errorInfo.Message);

        // Retry loading after 3 seconds
        Invoke(nameof(LoadInterstitial), 3f);
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial displayed");
    }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        isInterstitialReady = false;
        Debug.LogError("Interstitial failed to display: " + errorInfo.Message);
        LoadInterstitial();
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Interstitial clicked");
    }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isInterstitialReady = false;
        Debug.Log("Interstitial hidden");

        // Notify game that interstitial closed
        OnInterstitialClosed?.Invoke();

        // Load next interstitial
        LoadInterstitial();
    }

    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"Interstitial revenue: ${adInfo.Revenue} from {adInfo.NetworkName}");
    }

    #endregion

    #region Rewarded Ads

    private void InitializeRewardedAds()
    {
        // Rewarded ad callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

        // Load the first rewarded ad
        LoadRewardedAd();
    }

    private void LoadRewardedAd()
    {
        MaxSdk.LoadRewardedAd(rewardedAdUnitId);
    }

    public void ShowRewardedAd()
    {
        if (isRewardedReady)
        {
            MaxSdk.ShowRewardedAd(rewardedAdUnitId);
        }
        else
        {
            Debug.Log("Rewarded ad not ready");
            OnRewardedAdComplete?.Invoke(false);
        }
    }

    public bool IsRewardedAdReady()
    {
        return isRewardedReady;
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isRewardedReady = true;
        Debug.Log("Rewarded ad loaded: " + adInfo.NetworkName);
    }

    private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        isRewardedReady = false;
        Debug.LogError("Rewarded ad failed to load: " + errorInfo.Message);

        // Retry loading after 3 seconds
        Invoke(nameof(LoadRewardedAd), 3f);
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded ad displayed");
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        isRewardedReady = false;
        Debug.LogError("Rewarded ad failed to display: " + errorInfo.Message);
        OnRewardedAdComplete?.Invoke(false);
        LoadRewardedAd();
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        isRewardedReady = false;
        Debug.Log("Rewarded ad hidden");
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"Rewarded ad reward received: {reward.Amount} {reward.Label}");
        OnRewardedAdComplete?.Invoke(true);
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log($"Rewarded ad revenue: ${adInfo.Revenue} from {adInfo.NetworkName}");
    }

    #region Public Helper Methods

    private bool isBannerVisible = false;

    public void ToggleBanner()
    {
        if (isBannerVisible)
        {
            HideBanner();
        }
        else
        {
            ShowBanner();
        }
    }

    public bool IsInterstitialReady()
    {
        return isInterstitialReady;
    }

    public void DisableAppOpenAdsTemporarily(float duration = 300f)
    {
        // Disable app open ads for a period (useful during gameplay)
        lastAppOpenAdTime = Time.realtimeSinceStartup;
        appOpenAdCooldown = duration;
        Debug.Log($"App open ads disabled for {duration} seconds");
    }

    #endregion // End of Public Helper Methods

    #endregion // End of Rewarded Ads

    #region Unity Cleanup

    private void OnDestroy()
    {
        // Clean up callbacks
        MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitializedEvent;

        // Banner
        MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent -= OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent;

        // Interstitial
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnInterstitialRevenuePaidEvent;

        // Rewarded
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedRewardEvent;

        // App Open
        if (enableAppOpenAds)
        {
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenAdFailedToDisplayEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAppOpenAdRevenuePaidEvent;

            Application.focusChanged -= OnApplicationFocus;
        }
    }

    #endregion
}
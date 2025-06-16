using System;
using System.Collections;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

/// <summary>
/// Production‑ready AppLovin MAX manager supporting Banner, Interstitial, Rewarded & App‑Open Ads
/// – Consent (GDPR/CCPA/ATT)
/// – RemoteConfig‑driven parameters (intervals, feature toggles, ad‑unit ids…)
/// – Exponential back‑off retry with jitter
/// – Revenue forwarding to Firebase Analytics (compile‑time flag FIREBASE_ANALYTICS)
/// </summary>
public class MaxAdsManager : MonoBehaviour
{
    #region Singleton ------------------------------------------------------
    public static MaxAdsManager I { get; private set; }
    void Awake()
    {
        if (I) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Config & helpers ----------------------------------------------
    private AdsRemoteConfig rc;                       // ScriptableObject (Resources/AdsRemoteConfig)
    private ConsentFlow consent;                  // Handles GDPR/CCPA/ATT
    private AdRetryHelper retryInter, retryReward, retryAoa;

    private float lastInterTime;
    private float lastAoaTime;

    private const string SdkKey = "YOUR_SDK_KEY";     // fallback – should live in RemoteConfig or Env

    #endregion

    #region State flags ----------------------------------------------------
    private bool bannerReady;
    private bool interReady;
    private bool rewardReady;
    private bool aoaReady;
    private bool showingAoa;
    private bool bannerVisible;
    #endregion

    // --------------------------------------------------------------------
    IEnumerator Start()
    {
        rc = Resources.Load<AdsRemoteConfig>("AdsRemoteConfig");
        if (!rc) // safety stub
        {
            rc = ScriptableObject.CreateInstance<AdsRemoteConfig>();
            Debug.LogWarning("[Ads] No AdsRemoteConfig asset found – using defaults");
        }

        retryInter = new AdRetryHelper(rc, LoadInterstitial);
        retryReward = new AdRetryHelper(rc, LoadReward);
        retryAoa = new AdRetryHelper(rc, LoadAoa);

        consent = FindObjectOfType<ConsentFlow>() ?? new GameObject("ConsentFlow").AddComponent<ConsentFlow>();
        consent.OnConsentResolved += _ => InitSdk();
        consent.StartFlow();

        yield return null;
    }

    void InitSdk()
    {
        // SDK key is now set in the AppLovin Integration Manager.
        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInit;
        MaxSdk.InitializeSdk();
    }

    void OnSdkInit(MaxSdkBase.SdkConfiguration cfg)
    {
        Debug.Log("[Ads] MAX SDK ready");
        if (rc.enableBanner) InitBanner();
        if (rc.enableInter) InitInterstitial();
        if (rc.enableReward) InitReward();
        if (rc.enableAOA) InitAoa();
    }

    #region Banner ---------------------------------------------------------
    void InitBanner()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += (_, info) => { bannerReady = true; LogLoaded("Banner", info); };
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (_, err) => Debug.LogWarning($"[Ads] Banner LF: {err.Message}");
        MaxSdkCallbacks.Banner.OnAdClickedEvent += (_, info) => Debug.Log("[Ads] Banner clicked");
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += ForwardRevenue;

        var cfg = new MaxSdkBase.AdViewConfiguration(rc.bannerPos);
        MaxSdk.CreateBanner(rc.bannerId, cfg);
        MaxSdk.SetBannerBackgroundColor(rc.bannerId, Color.clear);
        ShowBanner();
    }
    public void ShowBanner() { if (bannerReady && !bannerVisible) { MaxSdk.ShowBanner(rc.bannerId); bannerVisible = true; } }
    public void HideBanner() { if (bannerVisible) { MaxSdk.HideBanner(rc.bannerId); bannerVisible = false; } }
    #endregion

    #region Interstitial ---------------------------------------------------
    void InitInterstitial()
    {
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (_, err) => { interReady = false; retryInter.ScheduleRetry(this); LogFail("Inter", err); };
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (_, info) => Debug.Log("[Ads] Inter displayed");
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (_, err, info) => { interReady = false; retryInter.ScheduleRetry(this); LogFail("Inter Display", err); };
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (_, info) => { interReady = false; LoadInterstitial(); };
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (_, info) => Debug.Log("[Ads] Inter clicked");
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += ForwardRevenue;
        LoadInterstitial();
    }
    void OnInterLoaded(string id, MaxSdkBase.AdInfo info) { interReady = true; retryInter.Reset(); LogLoaded("Inter", info); }
    void LoadInterstitial() => MaxSdk.LoadInterstitial(rc.interstitialId);

    public bool ShowInterstitial()
    {
        if (!rc.enableInter) return false;
        if (Time.unscaledTime - lastInterTime < rc.interstitialInterval) return false;
        if (!MaxSdk.IsInterstitialReady(rc.interstitialId)) return false;
        MaxSdk.ShowInterstitial(rc.interstitialId);
        lastInterTime = Time.unscaledTime;
        return true;
    }
    #endregion

    #region Rewarded -------------------------------------------------------
    void InitReward()
    {
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (_, err) => { rewardReady = false; retryReward.ScheduleRetry(this); LogFail("Reward LF", err); };
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (_, info) => Debug.Log("[Ads] Reward display");
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (_, err, info) => { rewardReady = false; retryReward.ScheduleRetry(this); LogFail("Reward DF", err); };
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (_, info) => { rewardReady = false; LoadReward(); };
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (_, reward, info) =>
        {
            Debug.Log($"[Ads] Reward {reward.Amount} {reward.Label}");
            OnRewarded?.Invoke(true);
        };
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += ForwardRevenue;
        LoadReward();
    }
    void OnRewardLoaded(string id, MaxSdkBase.AdInfo info) { rewardReady = true; retryReward.Reset(); LogLoaded("Reward", info); }
    void LoadReward() => MaxSdk.LoadRewardedAd(rc.rewardedId);

    public bool ShowReward()
    {
        if (!rewardReady || !MaxSdk.IsRewardedAdReady(rc.rewardedId)) { OnRewarded?.Invoke(false); return false; }
        MaxSdk.ShowRewardedAd(rc.rewardedId); return true;
    }
    public static event Action<bool> OnRewarded;
    #endregion

    #region App‑Open -------------------------------------------------------
    void InitAoa()
    {
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += (_, info) => { aoaReady = true; retryAoa.Reset(); LogLoaded("AOA", info); };
        MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += (_, err) => { aoaReady = false; retryAoa.ScheduleRetry(this); LogFail("AOA LF", err); };
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += (_, info) => { showingAoa = true; Debug.Log("[Ads] AOA display"); };
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += (_, info) => { showingAoa = false; aoaReady = false; lastAoaTime = Time.unscaledTime; LoadAoa(); };
        MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += (_, err, info) => { showingAoa = false; aoaReady = false; retryAoa.ScheduleRetry(this); LogFail("AOA DF", err); };
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += ForwardRevenue;
        Application.focusChanged += OnFocus;
        LoadAoa();
    }
    void LoadAoa() => MaxSdk.LoadAppOpenAd(rc.appOpenId);
    void OnFocus(bool focus)
    {
        if (!focus) return;
        if (showingAoa) return;
        if (Time.unscaledTime - lastAoaTime < rc.aoaInterval) return;
        if (!MaxSdk.IsAppOpenAdReady(rc.appOpenId)) return;
        MaxSdk.ShowAppOpenAd(rc.appOpenId);
    }
    #endregion

    #region Revenue & Logs -------------------------------------------------
    void ForwardRevenue(string adUnitId, MaxSdkBase.AdInfo info)
    {
#if FIREBASE_ANALYTICS
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression",
            new Parameter("ad_platform", "AppLovin"),
            new Parameter("ad_unit_id", adUnitId),
            new Parameter("ad_format", info.AdFormat),
            new Parameter("network", info.NetworkName),
            new Parameter("value", info.Revenue),
            new Parameter("currency", "USD"));
#endif
        Debug.Log($"[Ads] Rev ${info.Revenue:0.000} – {info.NetworkName}/{adUnitId}");
    }
    void LogLoaded(string tag, MaxSdkBase.AdInfo info) => Debug.Log($"[Ads] {tag} loaded via {info.NetworkName}");
    void LogFail(string tag, MaxSdkBase.ErrorInfo err) => Debug.LogWarning($"[Ads] {tag} failed: {err.Code}/{err.Message}");
    #endregion

    #region Cleanup --------------------------------------------------------
    void OnDestroy()
    {
        if (I == this) I = null;
    }
    #endregion
}

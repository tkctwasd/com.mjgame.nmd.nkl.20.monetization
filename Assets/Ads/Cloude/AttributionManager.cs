using System;
using System.Collections.Generic;
using UnityEngine;
#if ADJUST
using com.adjust.sdk;
#endif
#if APPSFLYER
using AppsFlyerSDK;
#endif

/// <summary>
/// Attribution Manager supporting both Adjust and AppsFlyer
/// Handles attribution tracking, deep links, and ad revenue tracking
/// </summary>
public class AttributionManager : MonoBehaviour
{
    #region Singleton
    public static AttributionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Configuration
    [Header("Adjust Configuration")]
    public string adjustAppToken = "YOUR_ADJUST_TOKEN";
#if ADJUST
        public AdjustEnvironment adjustEnvironment = AdjustEnvironment.Production;
#else
    public string adjustEnvironment = "Production";
#endif

    [Header("AppsFlyer Configuration")]
    public string appsFlyerDevKey = "YOUR_APPSFLYER_DEV_KEY";
    public string appsFlyerAppId = "YOUR_APPSFLYER_APP_ID"; // iOS only

    [Header("Settings")]
    public bool useAdjust = true;
    public bool useAppsFlyer = true;
    public bool enableDebugLogs = false;
    #endregion

    #region Events
    public static event Action<Dictionary<string, object>> OnAttributionChanged;
    public static event Action<string> OnDeepLinkReceived;
    public static event Action OnConversionDataReceived;
    #endregion

    #region Properties
    public bool IsAdjustInitialized { get; private set; }
    public bool IsAppsFlyerInitialized { get; private set; }
    public Dictionary<string, object> AttributionData { get; private set; } = new Dictionary<string, object>();
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeAttribution();
    }

    void OnApplicationPause(bool pauseStatus)
    {
#if APPSFLYER
        if (useAppsFlyer)
        {
            AppsFlyer.OnApplicationPause(pauseStatus);
        }
#endif
    }
    #endregion

    #region Initialization
    void InitializeAttribution()
    {
        if (useAdjust)
        {
            InitializeAdjust();
        }

        if (useAppsFlyer)
        {
            InitializeAppsFlyer();
        }
    }

    void InitializeAdjust()
    {
#if ADJUST
        var adjustConfig = new AdjustConfig(adjustAppToken, adjustEnvironment);
        
        if (enableDebugLogs)
        {
            adjustConfig.setLogLevel(AdjustLogLevel.Verbose);
        }
        
        // Attribution callback
        adjustConfig.setAttributionChangedDelegate(OnAdjustAttributionChanged);
        
        // Event tracking callbacks
        adjustConfig.setEventSuccessDelegate(OnAdjustEventSuccess);
        adjustConfig.setEventFailureDelegate(OnAdjustEventFailure);
        
        // Deep link callback
        adjustConfig.setDeferredDeeplinkDelegate(OnAdjustDeeplink);
        
        // Ad Revenue tracking
        adjustConfig.setAdRevenueDelegate(OnAdjustAdRevenue);
        
        // COPPA compliance
        adjustConfig.setCoppaCompliantEnabled(false);
        
        Adjust.start(adjustConfig);
        IsAdjustInitialized = true;
        Debug.Log("[Attribution] Adjust initialized");
#else
        Debug.LogWarning("[Attribution] ADJUST symbol not defined");
#endif
    }

    void InitializeAppsFlyer()
    {
#if APPSFLYER
        AppsFlyer.setIsDebug(enableDebugLogs);
        
        // Attribution callbacks
        AppsFlyer.OnRequestResponse += OnAppsFlyerRequestResponse;
        AppsFlyer.OnInAppResponse += OnAppsFlyerInAppResponse;
        AppsFlyer.OnDeepLinkReceived += OnAppsFlyerDeepLink;
        
        // Initialize
#if UNITY_IOS
        AppsFlyer.initSDK(appsFlyerDevKey, appsFlyerAppId, this);
#else
        AppsFlyer.initSDK(appsFlyerDevKey, null, this);
#endif
        
        AppsFlyer.startSDK();
        IsAppsFlyerInitialized = true;
        Debug.Log("[Attribution] AppsFlyer initialized");
#else
        Debug.LogWarning("[Attribution] APPSFLYER symbol not defined");
#endif
    }
    #endregion

    #region Adjust Callbacks
#if ADJUST
    void OnAdjustAttributionChanged(AdjustAttribution attribution)
    {
        AttributionData.Clear();
        AttributionData["network"] = attribution.network ?? "Unknown";
        AttributionData["campaign"] = attribution.campaign ?? "Unknown";
        AttributionData["adgroup"] = attribution.adgroup ?? "Unknown";
        AttributionData["creative"] = attribution.creative ?? "Unknown";
        
        Debug.Log($"[Attribution] Adjust attribution: {attribution.network}");
        OnAttributionChanged?.Invoke(AttributionData);
        
        // Log to Firebase
        FirebaseManager.LogEvent("attribution_received",
            new Firebase.Analytics.Parameter("network", attribution.network ?? "Unknown"),
            new Firebase.Analytics.Parameter("campaign", attribution.campaign ?? "Unknown")
        );
    }

    void OnAdjustEventSuccess(AdjustEventSuccess eventSuccessData)
    {
        Debug.Log($"[Attribution] Adjust event success: {eventSuccessData.EventToken}");
    }

    void OnAdjustEventFailure(AdjustEventFailure eventFailureData)
    {
        Debug.LogWarning($"[Attribution] Adjust event failure: {eventFailureData.Message}");
    }

    bool OnAdjustDeeplink(string deeplink)
    {
        Debug.Log($"[Attribution] Adjust deeplink: {deeplink}");
        OnDeepLinkReceived?.Invoke(deeplink);
        return true; // true = SDK will launch the deep link
    }

    void OnAdjustAdRevenue(AdjustAdRevenue adRevenue)
    {
        Debug.Log($"[Attribution] Adjust ad revenue: {adRevenue.getRevenue()}");
    }
#endif
    #endregion

    #region AppsFlyer Callbacks
#if APPSFLYER
    void OnAppsFlyerRequestResponse(object sender, EventArgs e)
    {
        var args = e as AppsFlyerRequestEventArgs;
        Debug.Log($"[Attribution] AppsFlyer request response: {args.statusCode}");
        
        if (args.statusCode == 200)
        {
            OnConversionDataReceived?.Invoke();
        }
    }

    void OnAppsFlyerInAppResponse(object sender, EventArgs e)
    {
        var args = e as AppsFlyerRequestEventArgs;
        Debug.Log($"[Attribution] AppsFlyer in-app response: {args.statusCode}");
    }

    void OnAppsFlyerDeepLink(object sender, EventArgs e)
    {
        var args = e as AppsFlyerDeepLinkEventArgs;
        Debug.Log($"[Attribution] AppsFlyer deeplink: {args.deepLink}");
        OnDeepLinkReceived?.Invoke(args.deepLink.ToString());
    }
#endif
    #endregion

    #region Event Tracking
    public static void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (Instance == null) return;

        Instance.TrackAdjustEvent(eventName, parameters);
        Instance.TrackAppsFlyerEvent(eventName, parameters);
    }

    void TrackAdjustEvent(string eventName, Dictionary<string, object> parameters)
    {
#if ADJUST
        if (!useAdjust || !IsAdjustInitialized) return;
        
        // Map common events to Adjust tokens (you need to configure these in Adjust dashboard)
        var eventTokens = new Dictionary<string, string>
        {
            {"level_complete", "abc123"}, // Replace with your actual tokens
            {"purchase", "def456"},
            {"tutorial_complete", "ghi789"}
        };
        
        if (eventTokens.ContainsKey(eventName))
        {
            var adjustEvent = new AdjustEvent(eventTokens[eventName]);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.Value is string str)
                        adjustEvent.addCallbackParameter(param.Key, str);
                    else
                        adjustEvent.addCallbackParameter(param.Key, param.Value.ToString());
                }
            }
            
            Adjust.trackEvent(adjustEvent);
            Debug.Log($"[Attribution] Adjust event tracked: {eventName}");
        }
#endif
    }

    void TrackAppsFlyerEvent(string eventName, Dictionary<string, object> parameters)
    {
#if APPSFLYER
        if (!useAppsFlyer || !IsAppsFlyerInitialized) return;
        
        var eventValues = new Dictionary<string, string>();
        
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                eventValues[param.Key] = param.Value.ToString();
            }
        }
        
        AppsFlyer.sendEvent(eventName, eventValues);
        Debug.Log($"[Attribution] AppsFlyer event tracked: {eventName}");
#endif
    }
    #endregion

    #region Ad Revenue Tracking
    public static void TrackAdRevenue(string adNetwork, string adUnitId, string adFormat, double revenue, string currency = "USD")
    {
        if (Instance == null) return;

        Instance.TrackAdjustAdRevenue(adNetwork, adUnitId, adFormat, revenue, currency);
        Instance.TrackAppsFlyerAdRevenue(adNetwork, adUnitId, adFormat, revenue, currency);
    }

    void TrackAdjustAdRevenue(string adNetwork, string adUnitId, string adFormat, double revenue, string currency)
    {
#if ADJUST
        if (!useAdjust || !IsAdjustInitialized) return;
        
        var adRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);
        adRevenue.setRevenue(revenue, currency);
        adRevenue.setAdRevenueNetwork(adNetwork);
        adRevenue.setAdRevenueUnit(adUnitId);
        adRevenue.setAdRevenuePlacement(adFormat);
        
        Adjust.trackAdRevenue(adRevenue);
        Debug.Log($"[Attribution] Adjust ad revenue tracked: ${revenue:F4}");
#endif
    }

    void TrackAppsFlyerAdRevenue(string adNetwork, string adUnitId, string adFormat, double revenue, string currency)
    {
#if APPSFLYER
        if (!useAppsFlyer || !IsAppsFlyerInitialized) return;
        
        var adRevenueParams = new Dictionary<string, string>
        {
            {"af_ad_revenue", revenue.ToString("F6")},
            {"af_ad_revenue_currency", currency},
            {"af_ad_revenue_network", adNetwork},
            {"af_ad_revenue_unit", adUnitId},
            {"af_ad_revenue_placement", adFormat}
        };
        
        AppsFlyer.sendEvent("af_ad_revenue", adRevenueParams);
        Debug.Log($"[Attribution] AppsFlyer ad revenue tracked: ${revenue:F4}");
#endif
    }
    #endregion

    #region Purchase Tracking
    public static void TrackPurchase(string productId, double revenue, string currency = "USD", string receipt = null)
    {
        if (Instance == null) return;

        Instance.TrackAdjustPurchase(productId, revenue, currency, receipt);
        Instance.TrackAppsFlyerPurchase(productId, revenue, currency, receipt);
    }

    void TrackAdjustPurchase(string productId, double revenue, string currency, string receipt)
    {
#if ADJUST
        if (!useAdjust || !IsAdjustInitialized) return;
        
        // You need to create a purchase event token in Adjust dashboard
        var adjustEvent = new AdjustEvent("PURCHASE_TOKEN"); // Replace with actual token
        adjustEvent.setRevenue(revenue, currency);
        adjustEvent.addCallbackParameter("product_id", productId);
        
        if (!string.IsNullOrEmpty(receipt))
        {
            adjustEvent.setReceipt(receipt, "transaction_id"); // iOS
        }
        
        Adjust.trackEvent(adjustEvent);
        Debug.Log($"[Attribution] Adjust purchase tracked: {productId} - ${revenue}");
#endif
    }

    void TrackAppsFlyerPurchase(string productId, double revenue, string currency, string receipt)
    {
#if APPSFLYER
        if (!useAppsFlyer || !IsAppsFlyerInitialized) return;
        
        var purchaseParams = new Dictionary<string, string>
        {
            {"af_revenue", revenue.ToString("F2")},
            {"af_currency", currency},
            {"af_content_id", productId},
            {"af_content_type", "product"}
        };
        
        if (!string.IsNullOrEmpty(receipt))
        {
            purchaseParams["af_receipt_id"] = receipt;
        }
        
        AppsFlyer.sendEvent("af_purchase", purchaseParams);
        Debug.Log($"[Attribution] AppsFlyer purchase tracked: {productId} - ${revenue}");
#endif
    }
    #endregion

    #region Utility Methods
    public static string GetAttributionNetwork()
    {
        if (Instance?.AttributionData.ContainsKey("network") == true)
        {
            return Instance.AttributionData["network"].ToString();
        }
        return "Organic";
    }

    public static bool IsOrganicUser()
    {
        return GetAttributionNetwork() == "Organic" || string.IsNullOrEmpty(GetAttributionNetwork());
    }
    #endregion

    #region Cleanup
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion
}
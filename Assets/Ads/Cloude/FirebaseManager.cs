using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.RemoteConfig;
#endif

/// <summary>
/// Firebase Manager for Analytics, Crashlytics, Remote Config
/// Handles initialization, event logging, and remote config fetching
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    #region Singleton
    public static FirebaseManager Instance { get; private set; }
    
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

    #region Events
    public static event Action OnFirebaseInitialized;
    public static event Action<Dictionary<string, object>> OnRemoteConfigFetched;
    #endregion

    #region Properties
    public bool IsInitialized { get; private set; }
    public Dictionary<string, object> RemoteConfigValues { get; private set; } = new Dictionary<string, object>();
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        InitializeFirebase();
    }
    #endregion

    #region Firebase Initialization
    void InitializeFirebase()
    {
#if FIREBASE_ANALYTICS
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) 
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                InitializeAnalytics();
                InitializeCrashlytics();
                InitializeRemoteConfig();
                
                IsInitialized = true;
                Debug.Log("[Firebase] Initialized successfully");
                OnFirebaseInitialized?.Invoke();
            } 
            else 
            {
                Debug.LogError($"[Firebase] Could not resolve dependencies: {dependencyStatus}");
            }
        });
#else
        Debug.LogWarning("[Firebase] FIREBASE_ANALYTICS symbol not defined");
#endif
    }

    void InitializeAnalytics()
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        FirebaseAnalytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
        
        // Set default user properties
        FirebaseAnalytics.SetUserProperty("device_model", SystemInfo.deviceModel);
        FirebaseAnalytics.SetUserProperty("operating_system", SystemInfo.operatingSystem);
        FirebaseAnalytics.SetUserProperty("app_version", Application.version);
        
        Debug.Log("[Firebase] Analytics initialized");
#endif
    }

    void InitializeCrashlytics()
    {
#if FIREBASE_ANALYTICS
        Crashlytics.SetUserId(SystemInfo.deviceUniqueIdentifier);
        Crashlytics.SetCustomKey("device_model", SystemInfo.deviceModel);
        Crashlytics.SetCustomKey("app_version", Application.version);
        
        Debug.Log("[Firebase] Crashlytics initialized");
#endif
    }

    void InitializeRemoteConfig()
    {
#if FIREBASE_ANALYTICS
        var defaults = new Dictionary<string, object>
        {
            // Ads Config
            {"ads_banner_enabled", true},
            {"ads_interstitial_enabled", true},
            {"ads_rewarded_enabled", true},
            {"ads_app_open_enabled", true},
            {"ads_interstitial_interval", 40},
            {"ads_app_open_interval", 60},
            {"ads_max_retry", 5},
            {"ads_base_delay", 2.0f},
            
            // Game Config
            {"show_rate_popup", true},
            {"min_sessions_for_rate", 3},
            {"maintenance_mode", false}
        };

        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults).ContinueWith(task => {
            Debug.Log("[Firebase] Remote Config defaults set");
            FetchRemoteConfig();
        });
#endif
    }
    #endregion

    #region Remote Config
    public void FetchRemoteConfig()
    {
#if FIREBASE_ANALYTICS
        var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        fetchTask.ContinueWith(FetchComplete);
#endif
    }

#if FIREBASE_ANALYTICS
    void FetchComplete(System.Threading.Tasks.Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.LogWarning("[Firebase] Remote Config fetch was cancelled");
            return;
        }
        
        if (fetchTask.IsFaulted)
        {
            Debug.LogWarning("[Firebase] Remote Config fetch failed: " + fetchTask.Exception);
            return;
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info = remoteConfig.Info;
        
        if (info.LastFetchStatus == LastFetchStatus.Success)
        {
            remoteConfig.ActivateAsync().ContinueWith(task => {
                Debug.Log("[Firebase] Remote Config fetched and activated");
                
                // Cache values
                RemoteConfigValues.Clear();
                foreach (var key in remoteConfig.Keys)
                {
                    var configValue = remoteConfig.GetValue(key);
                    RemoteConfigValues[key] = GetTypedValue(configValue);
                }
                
                OnRemoteConfigFetched?.Invoke(RemoteConfigValues);
            });
        }
        else
        {
            Debug.LogWarning($"[Firebase] Remote Config fetch failed: {info.LastFetchStatus}");
        }
    }

    object GetTypedValue(ConfigValue configValue)
    {
        if (configValue.BooleanValue.HasValue) return configValue.BooleanValue.Value;
        if (configValue.LongValue.HasValue) return (int)configValue.LongValue.Value;
        if (configValue.DoubleValue.HasValue) return (float)configValue.DoubleValue.Value;
        return configValue.StringValue;
    }
#endif

    public T GetRemoteConfigValue<T>(string key, T defaultValue = default(T))
    {
        if (RemoteConfigValues.ContainsKey(key))
        {
            try
            {
                return (T)Convert.ChangeType(RemoteConfigValues[key], typeof(T));
            }
            catch
            {
                Debug.LogWarning($"[Firebase] Could not convert remote config value for key: {key}");
            }
        }
        return defaultValue;
    }
    #endregion

    #region Analytics Events
#if FIREBASE_ANALYTICS
    public static void LogEvent(string eventName, params Parameter[] parameters)
    {
        if (Instance?.IsInitialized == true)
        {
            FirebaseAnalytics.LogEvent(eventName, parameters);
            Debug.Log($"[Firebase] Event logged: {eventName}");
        }
    }
#else
    public static void LogEvent(string eventName, params object[] parameters)
    {
        // Firebase Analytics not enabled; do nothing
    }
#endif

    #if FIREBASE_ANALYTICS
        public static void LogAdImpression(string adFormat, string adNetwork, string adUnitId, double revenue)
        {
            LogEvent("ad_impression",
                new Parameter("ad_format", adFormat),
                new Parameter("ad_network", adNetwork),
                new Parameter("ad_unit_id", adUnitId),
                new Parameter("value", revenue),
                new Parameter("currency", "USD")
            );
        }
    
        public static void LogAdClick(string adFormat, string adNetwork, string adUnitId)
        {
            LogEvent("ad_click",
                new Parameter("ad_format", adFormat),
                new Parameter("ad_network", adNetwork),
                new Parameter("ad_unit_id", adUnitId)
            );
        }
    #else
        public static void LogAdImpression(string adFormat, string adNetwork, string adUnitId, double revenue)
        {
            // Firebase Analytics not enabled; do nothing
        }
    
        public static void LogAdClick(string adFormat, string adNetwork, string adUnitId)
        {
            // Firebase Analytics not enabled; do nothing
        }
    #endif

    #if FIREBASE_ANALYTICS
        public static void LogGameEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            var firebaseParams = new List<Parameter>();
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (param.Value is string str)
                        firebaseParams.Add(new Parameter(param.Key, str));
                    else if (param.Value is int intVal)
                        firebaseParams.Add(new Parameter(param.Key, intVal));
                    else if (param.Value is float floatVal)
                        firebaseParams.Add(new Parameter(param.Key, floatVal));
                    else if (param.Value is double doubleVal)
                        firebaseParams.Add(new Parameter(param.Key, doubleVal));
                }
            }
            
            LogEvent(eventName, firebaseParams.ToArray());
        }
    #else
        public static void LogGameEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            // Firebase Analytics not enabled; do nothing
        }
    #endif

    #if FIREBASE_ANALYTICS
        public static void LogLevelStart(int level)
        {
            LogEvent("level_start", new Parameter("level", level));
        }
    
        public static void LogLevelEnd(int level, bool success)
        {
            LogEvent("level_end", 
                new Parameter("level", level),
                new Parameter("success", success ? 1 : 0)
            );
        }
    
        public static void LogPurchase(string itemId, string itemName, double value, string currency = "USD")
        {
            LogEvent("in_app_purchase",
                new Parameter("item_id", itemId),
                new Parameter("item_name", itemName),
                new Parameter("value", value),
                new Parameter("currency", currency)
            );
        }
    #else
        public static void LogLevelStart(int level)
        {
            // Firebase Analytics not enabled; do nothing
        }
    
        public static void LogLevelEnd(int level, bool success)
        {
            // Firebase Analytics not enabled; do nothing
        }
    
        public static void LogPurchase(string itemId, string itemName, double value, string currency = "USD")
        {
            // Firebase Analytics not enabled; do nothing
        }
    #endif
    #endregion

    #region User Properties
    public static void SetUserProperty(string name, string value)
    {
#if FIREBASE_ANALYTICS
        if (Instance?.IsInitialized == true)
        {
            FirebaseAnalytics.SetUserProperty(name, value);
        }
#endif
    }

    public static void SetUserLevel(int level)
    {
        SetUserProperty("user_level", level.ToString());
    }

    public static void SetUserVIPStatus(bool isVIP)
    {
        SetUserProperty("vip_status", isVIP ? "true" : "false");
    }
    #endregion

    #region Crashlytics
    public static void LogError(string message, string stackTrace = null)
    {
#if FIREBASE_ANALYTICS
        if (Instance?.IsInitialized == true)
        {
            Crashlytics.Log(message);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                Crashlytics.LogException(new System.Exception(message + "\n" + stackTrace));
            }
        }
#endif
    }

    public static void LogCustomKey(string key, string value)
    {
#if FIREBASE_ANALYTICS
        if (Instance?.IsInitialized == true)
        {
            Crashlytics.SetCustomKey(key, value);
        }
#endif
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
using System;
using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.RemoteConfig;
#endif

public class FirebaseService : IFirebaseService
{
    private static FirebaseService _instance;
    public static FirebaseService Instance => _instance ?? (_instance = new FirebaseService());

    private bool initialized = false;
    private Dictionary<string, object> remoteConfigCache = new();

    public void Initialize()
    {
#if FIREBASE_ANALYTICS
        if (initialized) return;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                initialized = true;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.SetUserId(SystemInfo.deviceUniqueIdentifier);

                // Set basic info
                SetUserProperty("device_model", SystemInfo.deviceModel);
                SetUserProperty("app_version", Application.version);
                
                SetDefaults();
                Debug.Log("[FirebaseService] Initialized.");
            }
            else
            {
                Debug.LogError($"[FirebaseService] Failed to initialize: {task.Result}");
            }
        });
#endif
    }

    private void SetDefaults()
    {
#if FIREBASE_ANALYTICS
        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(new Dictionary<string, object>
        {
            {"ads_interstitial_interval", 40},
            {"ads_enable_banner", true},
            {"ads_use_consent", true},
        });
#endif
    }

    public void FetchRemoteConfig(Action onComplete)
    {
#if FIREBASE_ANALYTICS
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWith(fetch =>
        {
            if (fetch.IsCompletedSuccessfully)
            {
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWith(_ =>
                {
                    remoteConfigCache.Clear();
                    foreach (var key in FirebaseRemoteConfig.DefaultInstance.Keys)
                    {
                        remoteConfigCache[key] = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
                    }
                    onComplete?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("[FirebaseService] Fetch remote config failed");
                onComplete?.Invoke();
            }
        });
#else
        onComplete?.Invoke();
#endif
    }

    public T GetConfig<T>(string key, T defaultValue = default)
    {
        if (remoteConfigCache.TryGetValue(key, out var value))
        {
            try { return (T)Convert.ChangeType(value, typeof(T)); }
            catch { return defaultValue; }
        }
        return defaultValue;
    }

    public void LogEvent(string name, Dictionary<string, object> parameters = null)
    {
#if FIREBASE_ANALYTICS
        if (!initialized) return;
        var paramList = new List<Parameter>();

        if (parameters != null)
        {
            foreach (var kv in parameters)
            {
                paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
            }
        }

        FirebaseAnalytics.LogEvent(name, paramList.ToArray());
        Debug.Log($"[FirebaseService] Event: {name}");
#endif
    }

    public void LogAdImpression(string adFormat, string adNetwork, string adUnitId, double revenue)
    {
        LogEvent("ad_impression", new Dictionary<string, object>
        {
            { "ad_format", adFormat },
            { "ad_network", adNetwork },
            { "ad_unit_id", adUnitId },
            { "value", revenue },
            { "currency", "USD" }
        });
    }

    public void SetUserProperty(string name, string value)
    {
#if FIREBASE_ANALYTICS
        FirebaseAnalytics.SetUserProperty(name, value);
#endif
    }

    public void LogCrash(string message, string stackTrace = null)
    {
#if FIREBASE_ANALYTICS
        Crashlytics.Log(message);
        if (!string.IsNullOrEmpty(stackTrace))
            Crashlytics.LogException(new Exception(message + "\n" + stackTrace));
#endif
    }
}

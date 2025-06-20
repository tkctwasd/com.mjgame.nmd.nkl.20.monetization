using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

class FirebaseManager
{
    static bool _isFirebaseInitialized;
    public static bool IsFirebaseInitialized
    {
        get => _isFirebaseInitialized;
        private set
        {
            _isFirebaseInitialized = value;
            if(value)
            {
                _tcs?.TrySetResult(true);
            }
        }
    }

    private static TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

    private static TaskCompletionSource<bool> _tcsRemoteConfig = new TaskCompletionSource<bool>();
    static Dictionary<string, object> KeyValuePairs = new Dictionary<string, object>();
    static string pathLocal => Path.Combine(Application.persistentDataPath, "config_local");

    [RuntimeInitializeOnLoadMethod]
    static void InitFirebaseManager()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
          {
              Firebase.DependencyStatus dependencyStatus = task.Result;

              if(dependencyStatus == Firebase.DependencyStatus.Available)
              {
                  Wasd.Log("Firebase is available");
                  InitializeRemoteConfig();
                  InitializeAnalytics();
                  IsFirebaseInitialized = true;
              }
              else
              {
                  Debug.LogError(String.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
              }
          });
    }

    static void LoadDataDefault()
    {
        KeyValuePairs = new Dictionary<string, object>();
        // Load default data from Resources
        var data = Resources.Load<TextAsset>("RemoteConfig");
        if(data == null) return;
        var dataResource = JsonConvert.DeserializeObject<Dictionary<string, object>>(data.text);

        // Load default data from local
        if(!File.Exists(pathLocal))
        {
            SaveDataLocal(data.text);
            Wasd.Log("Create remote local");
        }

        var dataLocal = JsonConvert.DeserializeObject<Dictionary<string, object>>(GetDataLocal());
        if(dataLocal.Count < dataResource.Count)
        {
            dataLocal = dataResource;
            SaveDataLocal(data.text);
            Wasd.Log("Update remote local");
        }
        else
        {
            Wasd.Log("Load remote local");
        }

        foreach(var item in dataLocal)
        {
            KeyValuePairs.Add(item.Key, item.Value);
        }

    }

    static void SaveDataLocal(string data)
    {
        if(!API.Get<ServiceRemoteConfig>().IsEnableRemoteConfig) return;
        Wasd.Log("Save remote config");
        File.WriteAllText(pathLocal, data);
    }
    static string GetDataLocal()
    {
        return File.ReadAllText(pathLocal);
    }
    static void InitializeRemoteConfig()
    {
        LoadDataDefault();
        if(!API.Get<ServiceRemoteConfig>().IsEnableRemoteConfig)
        {
            Wasd.LogColor("RemoteConfig in editor is disabled", "red");
            ServiceRemoteConfig.InitializeDone();
            _tcsRemoteConfig.TrySetResult(true);
            return;
        }
        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(KeyValuePairs)
           .ContinueWithOnMainThread(task =>
           {
               Wasd.Log("RemoteConfig configured and ready");
               FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(FetchComplete);
           });
    }
    static void FetchComplete(Task fetchTask)
    {
        Wasd.Log("Firebase fetch completed");
        var info = FirebaseRemoteConfig.DefaultInstance.Info;
        switch(info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread((Task t) =>
                {
                    ServiceRemoteConfig.InitializeDone();
                    _tcsRemoteConfig.TrySetResult(true);

                    JObject data = new JObject();
                    foreach(var item in FirebaseRemoteConfig.DefaultInstance.AllValues)
                    {
                        data.Add(item.Key, item.Value.StringValue);
                    }
                    Wasd.Log("Fetch data success and saved remote config");
                    SaveDataLocal(data.ToString());
                });
                return;
            case LastFetchStatus.Failure:
                Wasd.Log("Fetch data failure");
                break;
        }
    }
    static void InitializeAnalytics()
    {
        Wasd.Log("Firebase enabling data collection");
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

        Wasd.Log("Firebase set user properties");
        // Set the user's sign up method.
        FirebaseAnalytics.SetUserProperty(
          FirebaseAnalytics.UserPropertySignUpMethod,
#if UNITY_ANDROID
          "Google");
#elif UNITY_IOS
            "Apple");
#else
            "Unity");
#endif
        // Set default session Duration values.
        FirebaseAnalytics.SetSessionTimeoutDuration(new TimeSpan(0, 30, 0));
    }

    public static bool TryGetValue<T>(string key, out T value)
    {
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            value = default;
            return false;
        }
        if(!ServiceRemoteConfig.IsRemoteConfigInitialized)
        {
            value = default;
            return false;
        }
        value = GetValue<T>(key);
        return true;
    }
    public static T GetValue<T>(string key)
    {
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            Wasd.LogError("No internet connection");
            if(KeyValuePairs.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            return default;
        }

        if(!ServiceRemoteConfig.IsRemoteConfigInitialized)
        {
            Wasd.LogError("Remote Config is not initialized");
            if(API.Get<ServiceRemoteConfig>().ConfigDefault.TryGetValue(key, out var value))
            {
                return value.ToObject<T>();
            }
            return default;
        }
        var configValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        switch(typeof(T))
        {
            case Type t when t == typeof(string):
                return (T)Convert.ChangeType(configValue.StringValue, typeof(T));
            case Type t when t == typeof(int):
                return (T)Convert.ChangeType((int)configValue.LongValue, typeof(T));
            case Type t when t == typeof(float):
                return (T)Convert.ChangeType((float)configValue.DoubleValue, typeof(T));
            case Type t when t == typeof(bool):
                return (T)Convert.ChangeType(configValue.BooleanValue, typeof(T));

            default:
                return default;
        }
    }

    public static Task WaitForFirebaseInitialized(CancellationToken cancellationToken = default)
    {
        if(_isFirebaseInitialized)
        {
            return Task.CompletedTask;
        }

        cancellationToken.Register(() => _tcs.TrySetCanceled());
        return _tcs.Task;
    }
    public static Task WaitForFirebaseRemoteConfig(CancellationToken cancellationToken = default)
    {
        if(ServiceRemoteConfig.IsRemoteConfigInitialized)
        {
            return Task.CompletedTask;
        }

        cancellationToken.Register(() => _tcsRemoteConfig.TrySetCanceled());
        return _tcsRemoteConfig.Task;
    }
}
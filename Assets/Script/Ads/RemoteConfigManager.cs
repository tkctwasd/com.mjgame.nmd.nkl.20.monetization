using Firebase.Extensions;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class RemoteConfigManager : MonoBehaviour
{
    public static event Action ActionFetchDone;
    private static Dictionary<string, object> remoteConfigData;
    public static bool IsRemoteConfigComplete = false;

    private void Awake()
    {
        LoadLocalData();
    }

    private void OnEnable()
    {
        FirebaseManager.InitDone += Init;
        ActionFetchDone += LocalRemoteConfigAndCompare;
    }

    private void OnDisable()
    {
        FirebaseManager.InitDone -= Init;
        ActionFetchDone -= LocalRemoteConfigAndCompare;
    }
    private void Init()
    {
        FetchDataAsync();
    }

    private void FetchDataAsync()
    {
        Debug.Log("Fetching data...");
        System.Threading.Tasks.Task fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(
            TimeSpan.Zero);
        fetchTask.ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.Log("Fetch canceled.".Message());
        }
        else if (fetchTask.IsFaulted)
        {
            Debug.Log("Fetch encountered an error.".Message());
        }
        else if (fetchTask.IsCompleted)
        {
            Debug.Log("Fetch completed successfully!".Message());
        }

        var info = FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                Debug.Log(String.Format("Remote data loaded and ready (last fetch time {0}).",
                                       info.FetchTime));
                break;
            case LastFetchStatus.Failure:
                switch (info.LastFetchFailureReason)
                {
                    case FetchFailureReason.Error:
                        Debug.Log("Fetch failed for unknown reason");
                        break;
                    case FetchFailureReason.Throttled:
                        Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
                        break;
                }
                break;
            case LastFetchStatus.Pending:
                Debug.Log("Latest Fetch call still pending.");
                break;
        }
         #if UNITY_ANDROID || UNITY_IOS
                 ActionFetchDone?.Invoke();
         #endif
        // #if !UNITY_EDITOR
        //                         ActionFetchDone?.Invoke();
        // #endif
    }

    private void LoadLocalData()
    {
        string platformName = "RemoteConfig";
#if UNITY_ANDROID
        platformName = "RemoteConfig_Android";
#elif UNITY_IOS
        platformName = "RemoteConfig_iOS";
#endif
        TextAsset jsonFile = Resources.Load<TextAsset>(platformName);

        if (jsonFile == null)
        {
            Debug.LogError("RemoteConfig JSON not found in Resources: " + platformName);
            return;
        }
        try
        {
            remoteConfigData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonFile.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing or comparing RemoteConfig: " + ex.Message);
        }
        Debug.Log("FirebaseRemoteConfig".Ads("Load Local Data Json Complete"));
    }

    void LocalRemoteConfigAndCompare()
    {
        try
        {
            List<string> changedKeys = new List<string>();

            foreach (var key in remoteConfigData.Keys.ToList())
            {
                string localValue = remoteConfigData[key]?.ToString() ?? "";
                string remoteValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue ?? "";

                string normalizedLocal = RemoveAllWhitespace(localValue);
                string normalizedRemote = RemoveAllWhitespace(remoteValue);

                if (!string.IsNullOrEmpty(remoteValue) && normalizedLocal != normalizedRemote)
                {
                    changedKeys.Add(key);
                    Debug.Log($"<color=orange>[DIFF] </color> Key: {key} | <color=green>Local</color>: {localValue} <color=white>Remote</color>: {remoteValue}");
                    remoteConfigData[key] = remoteValue;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error parsing or comparing RemoteConfig: " + ex.Message);
        }
        IsRemoteConfigComplete = true;
        Debug.Log("FirebaseRemoteConfig".Ads("Load Complete"));
    }
    string RemoveAllWhitespace(string input)
    {
        return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static T GetConfigValue<T>(string key)
    {
        if (remoteConfigData == null || !remoteConfigData.ContainsKey(key))
            return default;

        try
        {
            object data = remoteConfigData[key];

            if (data is T tValue)
                return tValue;

            if (data is JObject jObject)
            {
                string jsonStr = jObject.ToString();
                return JsonConvert.DeserializeObject<T>(jsonStr);
            }

            string strVal = data?.ToString();

            if (typeof(T) == typeof(string))
                return (T)(object)strVal;

            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(strVal, out bool b))
                    return (T)(object)b;
                if (long.TryParse(strVal, out long l))
                    return (T)(object)(l != 0);
            }

            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(strVal, out int i))
                    return (T)(object)i;
                if (long.TryParse(strVal, out long l))
                    return (T)(object)(int)l;
            }

            if (typeof(T).IsClass && strVal.TrimStart().StartsWith("{"))
            {
                return JsonConvert.DeserializeObject<T>(strVal);
            }

            return (T)Convert.ChangeType(data, typeof(T));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigManager] Error parsing key '{key}' to type {typeof(T).Name}: {ex.Message}");
            return default;
        }
    }

    public static Dictionary<string, string> GetAllRemoteConfigValues()
    {
        var allKeys = FirebaseRemoteConfig.DefaultInstance.Keys;
        var result = new Dictionary<string, string>();

        foreach (var key in allKeys)
        {
            var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            result[key] = value;
        }

        return result;
    }
    public static Dictionary<string, object> DataRemoteConfig => remoteConfigData;
}

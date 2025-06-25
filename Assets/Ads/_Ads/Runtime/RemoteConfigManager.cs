using Firebase.Extensions;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class RemoteConfigManager : MonoBehaviour
{
    public static event Action FetchDone;
    private static Dictionary<string, object> remoteConfigData;

    private void Start()
    {
        FetchDone += LoadLocalRemoteConfigAndCompare;
    }
    private void OnEnable()
    {
        FirebaseManager.InitDone += Init;
    }

    private void OnDisable()
    {
        FirebaseManager.InitDone -= Init;
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
        if(fetchTask.IsCanceled)
        {
            Debug.Log("Fetch canceled.");
        }
        else if(fetchTask.IsFaulted)
        {
            Debug.Log("Fetch encountered an error.");
        }
        else if(fetchTask.IsCompleted)
        {
            Debug.Log("Fetch completed successfully!");
        }

        var info = FirebaseRemoteConfig.DefaultInstance.Info;
        switch(info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                Debug.Log(String.Format("Remote data loaded and ready (last fetch time {0}).",
                                       info.FetchTime));
                break;
            case LastFetchStatus.Failure:
                switch(info.LastFetchFailureReason)
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
        if(FetchDone != null) FetchDone.Invoke();
    }

    void LoadLocalRemoteConfigAndCompare()
    {
        string platformName = "RemoteConfig_".PlatformName().Replace(".json", "");
        TextAsset jsonFile = Resources.Load<TextAsset>(platformName);

        if(jsonFile == null)
        {
            Debug.LogError("RemoteConfig JSON not found in Resources: " + platformName);
            return;
        }

        try
        {
            remoteConfigData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonFile.text);
            List<string> changedKeys = new List<string>();

            foreach(var key in remoteConfigData.Keys.ToList())
            {
                string localValue = remoteConfigData[key]?.ToString() ?? "";
                string remoteValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue ?? "";

                string normalizedLocal = RemoveAllWhitespace(localValue);
                string normalizedRemote = RemoveAllWhitespace(remoteValue);

                if(!string.IsNullOrEmpty(remoteValue) && normalizedLocal != normalizedRemote)
                {
                    changedKeys.Add(key);
                    Debug.Log($"[DIFF] Key: {key} | Local: {localValue} Remote: {remoteValue}");
                    remoteConfigData[key] = remoteValue;
                }
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError("Error parsing or comparing RemoteConfig: " + ex.Message);
        }
    }
    string RemoveAllWhitespace(string input)
    {
        return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static T GetConfigValue<T>(string key)
    {
        if(remoteConfigData == null)
        {
            return default(T);
        }

        if(remoteConfigData.Count == 0)
        {
            return default(T);
        }

        if(remoteConfigData.ContainsKey(key))
        {
            try
            {
                object value = remoteConfigData[key];
                return (T)System.Convert.ChangeType(value, typeof(T));
            }
            catch(System.Exception ex)
            {
                Debug.LogError("Error parsing RemoteConfig: " + ex.Message);
                return default(T);
            }
        }
        else
        {
            return default(T);
        }
    }
}

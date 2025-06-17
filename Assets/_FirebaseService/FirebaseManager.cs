using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using FirebaseModules.RemoteConfig;
using Firebase.RemoteConfig;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Remote Config Settings")]
    [Tooltip("Tên file JSON trong Resources (không có .json)")]
    public string remoteConfigDefaultFile = "remote_config";

    [Tooltip("Số giây cache khi Fetch remote config")]
    public uint remoteConfigCacheSeconds = 0;

    public static bool IsFirebaseReady { get; private set; } = false;

    public static event Action OnFirebaseReady;

    private async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeFirebaseAsync();
    }

    private async Task InitializeFirebaseAsync()
    {
        Debug.Log("<color=yellow><b>[Firebase]</b></color> Khởi tạo Firebase...");

        var deps = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (deps != DependencyStatus.Available)
        {
            Debug.LogError($"<color=yellow><b>[Firebase]</b></color> Firebase dependency error: {deps}");
            return;
        }

        await RemoteConfigService.InitializeAsync(remoteConfigDefaultFile, remoteConfigCacheSeconds);

        IsFirebaseReady = true;
        OnFirebaseReady?.Invoke();

        Debug.Log("<color=yellow><b>[Firebase]</b><color> Sẵn sàng.");
        RemoteConfigService.LogAllValues();
    }
}

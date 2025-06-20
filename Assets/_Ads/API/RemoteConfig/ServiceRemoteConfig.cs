using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ServiceRemoteConfig : BaseModule
{
    public JObject ConfigDefault { get; private set; }
    static IRemoteConfigService remoteConfigService;
    static bool _isRemoteConfigInitialized;
    public static bool IsRemoteConfigInitialized
    {
        get => _isRemoteConfigInitialized;
        private set
        {
            _isRemoteConfigInitialized = value;
        }
    }
    public bool IsEnableRemoteConfig
    {
        get
        {
#if UNITY_EDITOR
            return PlayerPrefs.GetInt("isEnableRemoteConfig", 1) == 1;
#else
            return true;
#endif
        }
    }
    public override void Initialize()
    {
        var data = Resources.Load<TextAsset>("RemoteConfig");
        if(data != null)
        {
            ConfigDefault = JObject.Parse(data.text);
        }
    }
    public override void SetInfo() { }
    public override void Dispose() { }
    public static void Register(IRemoteConfigService _remoteConfigService)
    {
        remoteConfigService = _remoteConfigService;
        remoteConfigService.Initialize();
    }
    public Task FetchRemoteConfig()
    {
        if(remoteConfigService == null)
        {
            IsRemoteConfigInitialized = true;
            return Task.CompletedTask;
        }
        return remoteConfigService.FetchRemoteConfig();
    }
    public T GetValue<T>(string key)
    {
        if(remoteConfigService == null)
        {
            return default;
        }
        return remoteConfigService.GetValue<T>(key);
    }
    public bool TryGetValue<T>(string key, out T value)
    {
        if(remoteConfigService == null)
        {
            value = default;
            return false;
        }
        value = remoteConfigService.GetValue<T>(key);
        return true;
    }
    public static void InitializeDone()
    {
        IsRemoteConfigInitialized = true;
    }
}
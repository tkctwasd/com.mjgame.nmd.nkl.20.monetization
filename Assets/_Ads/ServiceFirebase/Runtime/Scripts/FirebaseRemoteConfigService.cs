using System.Threading.Tasks;
using UnityEngine;

public class FirebaseRemoteConfigService : IRemoteConfigService
{
    [RuntimeInitializeOnLoadMethod]
    static void InitFirebaseRemoteConfig()
    {
        var remoteConfig = new FirebaseRemoteConfigService();
        ServiceRemoteConfig.Register(remoteConfig);
    }
    public Task FetchRemoteConfig()
    {
        return FirebaseManager.WaitForFirebaseRemoteConfig();
    }

    public T GetValue<T>(string key)
    {
        return FirebaseManager.GetValue<T>(key);
    }

    public void Initialize()
    {
        Wasd.LogStateInitialize("FirebaseRemoteConfig");
    }
}

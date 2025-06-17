using System;
using System.Collections;
using UnityEngine;

public static class RemoteConfigLoader
{
    // Giả lập load remote config, thực tế có thể dùng Firebase RemoteConfig hoặc REST API
    public static IEnumerator Load(Action<AdsRemoteConfig> onLoaded)
    {
        yield return new WaitForSeconds(1f); // giả lập delay
        // Có thể lấy từ ScriptableObject local nếu cần test
        var so = Resources.Load<AdsRemoteConfigSO>("AdsRemoteConfigSO");
        if (so != null)
            onLoaded?.Invoke(so.config);
        else
            onLoaded?.Invoke(new AdsRemoteConfig());
    }
}

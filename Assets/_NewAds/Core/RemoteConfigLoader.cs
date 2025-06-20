using System;
using System.Collections;
using UnityEngine;

public static class RemoteConfigLoader
{
    // Giả lập load remote config, thực tế có thể dùng Firebase RemoteConfig hoặc REST API
    public static IEnumerator LoadStart(Action<AdsRemoteConfig> onLoaded)
    {
        yield return new WaitForSeconds(1f); // giả lập delay
        // Có thể lấy từ ScriptableObject local nếu cần test
        var so = Resources.Load<AdsRemoteConfigSO>("AdsRemoteConfigSO");
        if (so != null)
            onLoaded?.Invoke(so.config);
        else
            onLoaded?.Invoke(new AdsRemoteConfig());
    }

    // Load một key hoặc nhóm key động trong runtime (giả lập, thực tế có thể dùng API hoặc Firebase)
    public static IEnumerator LoadRuntime(string key, Action<object> onLoaded)
    {
        yield return new WaitForSeconds(0.5f); // giả lập delay runtime
                                               // Ở đây bạn có thể gọi API hoặc Firebase để lấy giá trị key động
                                               // Demo: lấy từ PlayerPrefs hoặc trả về giá trị giả lập
        object value = null;
        switch (key)
        {
            case "timeShowJetpack":
                value = 15f; // ví dụ giá trị runtime
                break;
            // Thêm các key runtime khác ở đây
            default:
                value = null;
                break;
        }
        onLoaded?.Invoke(value);
    }
}

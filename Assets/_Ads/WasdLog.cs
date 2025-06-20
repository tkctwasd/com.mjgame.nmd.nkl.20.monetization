using UnityEngine;

public static class Wasd
{
    public static void Log(object message, string title = "")
    {

#if UNITY_EDITOR
        if(title != "")
            Debug.Log($"<color=green>[WASD]</color> |<color=#7f6e46>{title}</color>| {message}");
        else
            Debug.Log($"<color=green>[WASD]</color> {message}");
#elif UNITY_IOS
        if(title != "")
            Debug.Log($"[WASD] |{title}| {message}");
        else
            Debug.Log($"[WASD] {message}");
#else
        if(title != "")
            Debug.Log($"[WASD] |{title}| {message}");
        else
            Debug.Log($"[WASD] {message}");
#endif
    }
    public static void LogColor(object message, string color)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=green>[WASD]</color> <color={color}>{message}</color>");
#elif UNITY_IOS
       Log(message, "");
#endif
    }
    public static void LogStateInitialize(string service)
    {
#if UNITY_EDITOR
        Debug.Log($"<color=green>[WASD]</color> |<color=#FFA500>{service}</color>| Initialized!");
#elif UNITY_IOS
        Debug.Log($"[WASD] |{service}| Initialized!");
#else
        Debug.Log($"[WASD] |{service}| Initialized!");
#endif
    }
    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning($"<color=yellow>[WASD]</color> {message}");
#elif UNITY_IOS
        Debug.LogWarning($"[WASD] {message}");
#else
        Debug.LogWarning($"[WASD] {message}");
#endif
    }
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError($"<color=red>[WASD]</color> {message}");
#elif UNITY_IOS
        Debug.LogError($"[WASD] {message}");
#else
        Debug.LogError($"[WASD] {message}");
#endif
    }
}

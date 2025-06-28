using UnityEngine;

public static class LogMessage
{
    public static void Log(string content, string module = "", string type = "[Ads]")
    {
        Debug.Log($"<color=yellow>{type}</color> <color=green>[{module}]</color>: " + content);
    }
}

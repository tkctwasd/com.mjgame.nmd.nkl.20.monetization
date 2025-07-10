using UnityEngine;

public static class LogMessage
{
    public static void Log(string content, string module = "", string type = "[Ads]")
    {
        Debug.Log($"<color=yellow>{type}</color> <color=green>[{module}]</color>: " + content);
    }


    public static string Ads(this string module, string message = "")
    {
        return $"<b><color=green>[Ads]</color> <color=orange>[{module}]</color></b> {message}";
    }

    public static string Message(this string component, string message = "")
    {
        return $"<color=yellow><b>[{component}]</b></color> {message}";
    }

    public static string Message(this string message)
    {
        return $"<color=green><b>[{message}]</b></color>";
    }

}

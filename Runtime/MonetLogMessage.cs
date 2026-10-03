public static class MonetLogMessage
{
    public static string Ads(this string module, string message = "")
    {
        return $"<b><color=#19FF00>[Ads]</color> <color=#0098FF>[{module}]</color></b> {message}";
    }

    public static string IAP(this string module, string message = "")
    {
        return $"<b><color=#FF00AF>[IAP]</color> <color=#9D00FF>[{module}]</color></b> {message}";
    }

    public static string Firebase(this string module, string message = "")
    {
        return $"<b><color=#FF9500>[Firebase]</color> <color=#FF4500>[{module}]</color></b> {message}";
    }
}

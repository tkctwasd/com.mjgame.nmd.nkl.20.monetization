using UnityEngine.Analytics;
using System.Collections.Generic;

public class UnityAnalytics : IAdsAnalytics
{
    public void LogEvent(string eventName, object data = null)
    {
        // Nếu data là Dictionary<string, object> hoặc Dictionary<string, string>
        if (data is Dictionary<string, object> dictObj)
        {
            Analytics.CustomEvent(eventName, dictObj);
        }
        else if (data is Dictionary<string, string> dictStr)
        {
            // Chuyển sang Dictionary<string, object> để Unity Analytics nhận
            var dictObj2 = new Dictionary<string, object>();
            foreach (var kv in dictStr)
                dictObj2[kv.Key] = kv.Value;
            Analytics.CustomEvent(eventName, dictObj2);
        }
        else
        {
            Analytics.CustomEvent(eventName);
        }
    }
}
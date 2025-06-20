using System.Collections.Generic;

namespace game.analytics
{
    public class AnalyticsManager
    {
        protected static List<IAnalytics> Analytics { get; } = new List<IAnalytics>();

        public static void AddAnalytics(IAnalytics analytics)
        {
            if (!Analytics.Contains(analytics))
                Analytics.Add(analytics);
        }
        public static void LogEvent(string eventName, Dictionary<string, object> eventParams)
        {
            foreach (var analytics in Analytics)
            {
                analytics.LogEvent(eventName, eventParams);
            }
        }
        public static void LogEvent(string eventName)
        {
            foreach (var analytics in Analytics)
            {
                analytics.LogEvent(eventName);
            }
        }
        public static T GetAnalytics<T>() where T : IAnalytics
        {
            foreach (var analytics in Analytics)
            {
                if (analytics is T)
                    return (T)analytics;
            }
            return default;
        }
    }
}
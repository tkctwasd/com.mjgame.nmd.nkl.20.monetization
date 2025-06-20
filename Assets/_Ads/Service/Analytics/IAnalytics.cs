using System.Collections.Generic;

namespace game.analytics
{
    public interface IAnalytics
    {
        public string Type { get; }
        public void LogEvent(string eventName, Dictionary<string, object> eventParams);
        public void LogEvent(string eventName);
    }
}

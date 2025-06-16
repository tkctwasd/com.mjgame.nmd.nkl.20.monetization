using System;
using System.Collections.Generic;

public interface IFirebaseService
{
    void Initialize();
    void LogEvent(string name, Dictionary<string, object> parameters = null);
    void LogAdImpression(string adFormat, string adNetwork, string adUnitId, double revenue);
    void LogCrash(string message, string stackTrace = null);
    void SetUserProperty(string name, string value);
    void FetchRemoteConfig(Action onComplete);
    T GetConfig<T>(string key, T defaultValue = default);
}

using System.Collections.Generic;

public interface ILogEventService
{
    void Initialize();
    void Log(LogEventParameter logEventParameter);
    void OnShowAppOpen(string placement);
    void OnAppOpenClick(string placement);
    void OnShowAppOpenSuccess(string placement);
    void OnBannerClick();
    void OnShowBanner();
    void OnMRecClick(string placement);
    void OnShowInterstitial(bool hasAds, string placement);
    void OnInterstitialClick(string placement);
    void OnInterstitialNumReach(int numReach);
    void OnShowInterstitialSuccess(string placement);
    void OnShowRewarded(bool hasAds, string placement);
    void OnRewardedClick(string placement);
    void OnRewardedNumReach(int numReach);
    void OnShowRewardedSuccess(string placement);
}
public class LogEventParameter
{
    public string Name { get; }
    public Dictionary<string, object> Params { get; private set; } = new Dictionary<string, object>();
    public LogEventParameter(string name)
    {
        this.Name = name;
    }

    public static LogEventParameter Create(string name)
    {
        return new LogEventParameter(name);
    }

    public LogEventParameter AddParam(string key, object value)
    {
        if(Params.ContainsKey(key))
        {
            Params[key] = value;
        }
        else
        {
            Params.Add(key, value);
        }
        return this;
    }

    public void Log()
    {
        API.Get<ServiceLogEvent>().Log(this);
    }
}

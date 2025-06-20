using Firebase.Analytics;
using System.Collections.Generic;
using UnityEngine;

public class FirebaseLogEventService : ILogEventService
{
    [RuntimeInitializeOnLoadMethod]
    static void InitFirebaseAnalytics()
    {
        var logService = new FirebaseLogEventService();
        ServiceLogEvent.Register(logService);
        logService.Initialize();
    }
    public async void Initialize()
    {
        await FirebaseManager.WaitForFirebaseInitialized();
        Wasd.LogStateInitialize("FirebaseAnalytics");
    }
    Parameter[] GetParameters(LogEventParameter parameter)
    {
        List<Parameter> list = new List<Parameter>();
        if(parameter == null)
        {
            return list.ToArray();
        }
        foreach(var item in parameter.Params)
        {
            switch(item.Value)
            {
                case int i: list.Add(new Parameter(item.Key, i)); break;
                case float f: list.Add(new Parameter(item.Key, f)); break;
                case double d: list.Add(new Parameter(item.Key, d)); break;
                default: list.Add(new Parameter(item.Key, item.Value.ToString())); break;
            };
        }
        return list.ToArray();
    }
    public void Log(string eventName)
    {
        LogEventParameter parameter = new LogEventParameter(eventName);
        Log(parameter);
    }
    public void Log(string eventName, params (string key, object value)[] parameters)
    {
        LogEventParameter parameter = new LogEventParameter(eventName);
        foreach(var item in parameters)
        {
            parameter.AddParam(item.key, item.value);
        }
        Log(parameter);
    }
    public void Log(LogEventParameter parameter)
    {
        var parameters = GetParameters(parameter);
        if(Debug.isDebugBuild)
            Debug.Log($"W_LogEvent: {parameter.Name}");

        switch(parameters.Length)
        {
            case 0:
                FirebaseAnalytics.LogEvent(parameter.Name);
                break;
            case 1:
                FirebaseAnalytics.LogEvent(parameter.Name, parameters[0]);
                break;
            default:
                FirebaseAnalytics.LogEvent(parameter.Name, parameters);
                break;
        }
    }

    #region APP_OPEN
    public void OnShowAppOpen(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_app_open_ads")
            .AddParam("placement", placement);
        Log(parameter);
    }

    public void OnAppOpenClick(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_app_open_ads_click")
            .AddParam("placement", placement);
        Log(parameter);
    }
    public void OnShowAppOpenSuccess(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_app_open_ads_success")
            .AddParam("placement", placement);
        Log(parameter);
    }
    #endregion
    #region BANNER
    public void OnShowBanner()
    {
        Log("show_banner_ads");
    }
    public void OnBannerClick()
    {
        Log("show_banner_click");
    }
    #endregion
    #region MREC
    public void OnMRecClick(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_mrec_click")
         .AddParam("placement", placement);
        Log(parameter);
    }
    #endregion

    #region INTERSTITIAL
    public void OnShowInterstitial(bool hasAds, string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_interstitial_ads")
            .AddParam("has_ads", hasAds.ToString())
            .AddParam("placement", placement);
        Log(parameter);
    }
    public void OnInterstitialNumReach(int numReach)
    {
        LogEventParameter parameter = new LogEventParameter("impdau_inter_passed")
            .AddParam("ImpdauPassed", numReach);
        Log(parameter);
    }
    public void OnInterstitialClick(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_interstitial_ads_click")
            .AddParam("placement", placement);
        Log(parameter);
    }
    public void OnShowInterstitialSuccess(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_interstitial_ads_success")
            .AddParam("placement", placement);
        Log(parameter);
    }
    #endregion

    #region REWARDED
    public void OnShowRewarded(bool hasAds, string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_rewarded_ads")
            .AddParam("has_ads", hasAds.ToString())
            .AddParam("placement", placement);
        Log(parameter);
    }
    public void OnRewardedClick(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_rewarded_ads_click")
            .AddParam("placement", placement);
        Log(parameter);
    }

    public void OnRewardedNumReach(int numReach)
    {
        LogEventParameter parameter = new LogEventParameter("impdau_rewarded_passed")
            .AddParam("ImpdauPassed", numReach);
        Log(parameter);
    }

    public void OnShowRewardedSuccess(string placement)
    {
        LogEventParameter parameter = new LogEventParameter("show_rewarded_ads_success")
            .AddParam("placement", placement);
        Log(parameter);
    }
  
    #endregion
}
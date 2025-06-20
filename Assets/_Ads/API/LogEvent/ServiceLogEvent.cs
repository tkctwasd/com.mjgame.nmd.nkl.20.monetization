using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ServiceLogEvent : BaseModule
{
    static ILogEventService logEventService;
    public override void Initialize() { }
    public override void SetInfo() { }
    public override void Dispose() { }
    public static void Register(ILogEventService _logEventService)
    {
        logEventService = _logEventService;
    }
    string GetName(string name)
    {
        var result = Regex.Replace(name, @"[^0-9a-zA-Z]+", "_");
        return result.ToLower();
    }
    #region LOG_EVENT
    public void Log(LogEventParameter parameter)
    {
        if(string.IsNullOrEmpty(parameter.Name)) return;
        logEventService?.Log(parameter);
    }
    public void Log(string name)
    {
        if(string.IsNullOrEmpty(name)) return;
        logEventService?.Log(LogEventParameter.Create(name));
    }
    public void Log(string eventName, params (string key, object value)[] parameters)
    {
        if(string.IsNullOrEmpty(eventName)) return;
        LogEventParameter parameter = new LogEventParameter(eventName);
        foreach(var item in parameters)
        {
            parameter.AddParam(item.key, item.value);
        }
        Log(parameter);
    }
    #endregion

    #region LOG_ADS  

    #region APP_OPEN

    public void OnShowAppOpen(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowAppOpen(GetName(placement));
    }
    public void OnShowAppOpenSuccess(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowAppOpenSuccess(GetName(placement));
    }
    public void OnAppOpenClick(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnAppOpenClick(GetName(placement));
    }
    #endregion

    #region BANNER
    public void OnShowBanner()
    {
        logEventService?.OnShowBanner();
    }
    public void OnBannerClick()
    {
        logEventService?.OnBannerClick();
    }
    #endregion
    #region INTERSTITIAL
    public void OnShowInterstitial(bool hasAds, string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowInterstitial(hasAds, GetName(placement));
    }
    public void OnShowInterstitialSuccess(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowInterstitialSuccess(GetName(placement));
    }
    public void OnInterstitialNumReach(int numReach)
    {
        logEventService?.OnInterstitialNumReach(numReach);
    }
    public void OnInterstitialClick(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnInterstitialClick(GetName(placement));
    }
    #endregion

    #region REWARDED
    public void OnShowRewarded(bool hasAds, string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowRewarded(hasAds, GetName(placement));
    }
    public void OnShowRewardedAdsSuccess(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnShowRewardedSuccess(GetName(placement));
    }
    public void OnRewardedNumReach(int numReach)
    {
        logEventService?.OnRewardedNumReach(numReach);
    }
    public void OnRewardedClick(string placement)
    {
        if(string.IsNullOrEmpty(placement)) return;
        logEventService?.OnRewardedClick(GetName(placement));
    }

    #endregion
    #endregion
}
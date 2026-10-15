using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class AdsConfig
{
    public bool ShowAppOpen = true;
    public bool ShowInterstitial = true;
    public bool ShowBanner = true;
    public bool ShowMREC = true;
    public bool ShowReward = true;
    public int InterstitialCappingTime = 45;
    public int AppOpenCappingTime = 60;
    public int TimestampInterReward = 2;
    public int TimestampInterAppOpen = 2;
    public bool SwitchBannerMREC = false;
    public int DurationLoading = 10;
    public int TimeLoadAppOpen = 5;
    public int TimeDelayInterstitial = 5;
    public bool InterstitialOnClose = true;
}


public static class RemoteKey
{
    public static string AdsConfig = "AdsConfig";
    public static string PopupRate = "PopupRate";
}
[Serializable]
public class PopupRate
{
    public int MaxShowCount = 2;
    public bool CanShowRate = false;
    public bool ShowAppleStore = false;
}


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
    public int InterstitialCappingTime = 20;
    public int AppOpenCappingTime = 50;
    public int TimestampInterReward = 2;
    public int TimestampInterAppOpen = 2;
    public bool SwitchBannerMREC = false;
    public int DurationLoading = 10;
    public int TimeLoadAppOpen = 6;
    public int TimeDelayInterstitial = 5;
}


public static class RemoteKey
{
    public static string AdsConfig = "AdsConfig";
}
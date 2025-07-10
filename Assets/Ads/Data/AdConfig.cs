using System;
[Serializable]
public class AdsConfig
{
    public bool ShowInfo = true;
    public bool ShowAppOpen = true;
    public bool ShowInterstitial = true;
    public bool ShowBanner = true;
    public bool ShowReward = true;
    public int InterstitialCappingTime = 45;
    public int AppOpenCappingTime = 60;
    public int InterstitialIndexStart = 2;
}

[Serializable]
public class AdsPopupSetting
{
    public bool btnClose = false;
    public bool btnReset = false;
}

public static class RemoteKey
{
    public static string AdsConfig = "AdsConfig";
    public static string AdsPopupSetting = "AdsPopupSetting";
}
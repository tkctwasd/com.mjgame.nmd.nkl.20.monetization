using System;

[Serializable]
public class AdsSettings
{
    public int IntervalShowInterstitial = 40;
    public bool EnableAppOpenAd = true;
    public bool ShowBannerOnStart = true;
    public int RewardedCooldown = 60;
    public int AppOpenCooldown = 30;
    public int MaxRetryAttempts = 3;
    public bool EnableGDPRConsent = false;
    public string BannerPosition = "BottomCenter";
    public int InterstitialStartDelay = 5;
    public bool EnableDebugLog = true;
}
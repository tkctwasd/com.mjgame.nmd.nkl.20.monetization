using System;
using UnityEngine;

[Serializable]
public class AdsRemoteConfig
{
    public string SdkKey;
    public string BannerId;
    public string InterstitialId;
    public string RewardedId;
    public string AppOpenId;
    public bool EnableBanner = true;
    public bool EnableInterstitial = true;
    public bool EnableRewarded = true;
    public bool EnableAppOpen = true;
    public float InterstitialCooldown = 30f;
    public float AppOpenCooldown = 60f;
    // Có thể mở rộng thêm các tham số khác
}

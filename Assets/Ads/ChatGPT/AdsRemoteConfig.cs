using UnityEngine;

[CreateAssetMenu(menuName = "Ads/RemoteConfig")]
public class AdsRemoteConfig : ScriptableObject
{
    [Header("Intervals (seconds)")]
    public int interstitialInterval = 40;
    public int aoaInterval = 60;

    [Header("Retry")]
    public int maxRetry = 5;
    public float baseDelay = 2f;

    [Header("Placements & Units")]
    public string bannerId;
    public string interstitialId;
    public string rewardedId;
    public string appOpenId;
    public MaxSdkBase.AdViewPosition bannerPos = MaxSdkBase.AdViewPosition.BottomCenter;

    [Header("Feature Toggles")]
    public bool enableBanner = true;
    public bool enableInter = true;
    public bool enableReward = true;
    public bool enableAOA = true;
}

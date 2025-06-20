using System;

[Serializable]
public class AdsSetting
{
    public bool IsRemoveAds = false;
    public bool IsUseAppOpenAd = true;
    public bool IsShowBanner = true;
    public bool IsShowInterstitial = true;
    public bool IsShowAdBreak = true;
    public int MaxClickBanner = 3;
    public int MaxClickInterstitial = 3;
    public int MaxClickRewarded = 3;
    public float TimeLoadAppOpen = 15;
    public float InterstitialDelay = 0;
    public BannerPosition BannerPosition = BannerPosition.BottomCenter;
    public BannerColor BannerColor = BannerColor.Black;
    public float IntervalShowInterstitial = 20;
    public bool IsRefocusShowAds = true;
    public float IntervalRefocusShow = 5;   
}
[System.Serializable]
public enum BannerColor
{
    Black,
    NoColor
}

[Serializable]
public enum BannerPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    Centered,
    CenterLeft,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
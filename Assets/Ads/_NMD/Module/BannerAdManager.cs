namespace A
{
    using UnityEngine;
using static MaxSdkBase;

public class BannerAdManager
{
    // NEW: Tham chiếu đến ApplovinMaxManager (nếu cần tương tác ngược lại)
    private ApplovinMaxManager _applovinMaxManager;
    private string _adUnitId;
    private MaxSdkBase.AdViewPosition _bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;

    // Constructor: Khởi tạo Manager với tham chiếu và Ad Unit ID
    public BannerAdManager(ApplovinMaxManager applovinManager, string adUnitId)
    {
        _applovinMaxManager = applovinManager; // Gán tham chiếu
        _adUnitId = adUnitId;
    }

    public void Initialize()
    {
        LogW.Log($"Initializing Banner Ad with ID: {_adUnitId}", "Banner");

        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;

        var _adViewConfiguration = new AdViewConfiguration(_bannerPosition);
        MaxSdk.SetBannerWidth(_adUnitId, MaxSdkUtils.IsTablet() ? 800 : 350);
        MaxSdk.CreateBanner(_adUnitId, _adViewConfiguration);
        MaxSdk.SetBannerBackgroundColor(_adUnitId, Color.clear);

    }

    private bool IsShowBanner()
    {
        return RemoteConfigManager.GetConfigValue<bool>("Ad_Use_Banner");
    }

    public void ShowBanner()
    {
        if (IsShowBanner())
        {
            LogW.Log("Showing Banner Ad", "Banner");
            MaxSdk.ShowBanner(_adUnitId);
            FirebaseAnalyticManager.Instance.LogAd("show_banner_ads", "bottom_center");
        }
        else
        {
            LogW.Log("Banner Ad is disabled by Remote Config or IsShowBanner() is false.", "Banner");
        }
    }

    public void HideBanner()
    {
        LogW.Log("Hiding Banner Ad", "Banner");
        MaxSdk.HideBanner(_adUnitId);
    }

    private void OnBannerAdLoadedEvent(string adUnitId, AdInfo adInfo)
    {
        LogW.Log($"Banner Ad loaded. Source: {adInfo.NetworkName}", "Banner");
    }

    private void OnBannerAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
    {
        LogW.Log($"Banner Ad load failed: {errorInfo.Message}", "Banner");
    }

    private void OnBannerAdClickedEvent(string adUnitId, AdInfo adInfo)
    {
        LogW.Log($"Banner Ad clicked. Source: {adInfo.NetworkName}", "Banner");
        FirebaseAnalyticManager.Instance.LogAd("show_banner_ads_click", "bottom_center");
    }
}
}
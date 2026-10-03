using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class BannerModule
{
    AdsManager ApplovinManager;
    string bannerId = "banner-id";


    public BannerModule(AdsManager applovin, string bannerId)
    {
        ApplovinManager = applovin;
        this.bannerId = bannerId;
    }

    #region Banner

    public void Initialize()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
    }

    private IEnumerator IELoadDelay(float time)
    {
        yield return new WaitForSeconds(time);
        LoadBanner();
    }

    private void LoadBanner()
    {
        var adViewConfiguration = new AdViewConfiguration(ApplovinManager.AdConstants.bannerPosition);
        // MaxSdk.SetBannerWidth(bannerId, MaxSdkUtils.IsTablet() ? 800 : 350);
        MaxSdk.CreateBanner(bannerId, adViewConfiguration);
        MaxSdk.SetBannerBackgroundColor(bannerId, Color.clear);
    }

    public bool CanShowBanner()
    {
        if (ApplovinManager.IsRemoveAdsPurchased)
            return false;

        var config = AdsManager.GetAdsConfig();
        return config.ShowBanner;
    }


    public void ShowBanner()
    {
        if (CanShowBanner())
        {
            AdAnalytic.LogAd("show_banner_ads", "");
            MaxSdk.ShowBanner(bannerId);
        }
    }

    public void HideBanner()
    {
        MaxSdk.HideBanner(bannerId);
    }

    private void OnBannerAdLoadedEvent(string adUnitId, AdInfo adInfo)
    {
        Debug.Log("Banner".Ads($"Ad Loaded"));
    }

    private void OnBannerAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
    {
        Debug.Log("Banner".Ads($"Ad Load Failed: {errorInfo.Message}"));
    }

    private void OnBannerAdClickedEvent(string adUnitId, AdInfo adInfo)
    {
        AdAnalytic.LogAd("show_banner_ads_click", "");
    }

    private void OnBannerAdExpandedEvent(string adUnitId, AdInfo adInfo) { }

    private void OnBannerAdCollapsedEvent(string adUnitId, AdInfo adInfo) { }

    #endregion

    #region RevenuePaidEvent
    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        ApplovinManager.SendAdRevenueTracker(impressionData);
    }
    #endregion
}


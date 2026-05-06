using UnityEngine;
using static MaxSdkBase;
using System.Collections;

public class MRECModule
{
    AdsManager ApplovinManager;
    string mrecId = "mrec-id";

    public MRECModule(AdsManager applovin, string mrecId)
    {
        ApplovinManager = applovin;
        this.mrecId = mrecId;
    }

    #region MREC

    public void Initialize()
    {
        MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRECAdLoadedEvent;
        MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRECAdLoadFailedEvent;
        MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRECAdClickedEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMRECAdExpandedEvent;
        MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMRECAdCollapsedEvent;

        ApplovinManager.StartCoroutine(IELoadDelay(0.5f));
    }

    private IEnumerator IELoadDelay(float time)
    {
        yield return new WaitForSeconds(time);
        LoadMREC();
    }

    private void LoadMREC()
    {
        var adViewConfiguration = new AdViewConfiguration(ApplovinManager.AdConstants.mrecPosition);
        MaxSdk.CreateMRec(mrecId, adViewConfiguration);
    }


    public void ShowMREC()
    {
        var adsConfig = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (adsConfig.ShowMREC)
        {
            AdAnalytic.LogAd("show_mrec_ads", "");
            Debug.Log("MREC".Ads("Show"));
            ApplovinManager.ActiveMREC = true;
            MaxSdk.ShowMRec(mrecId);
            MaxSdkMREC.ShowMRec(mrecId, ApplovinManager.AdConstants.mrecPosition);
            if (adsConfig.SwitchBannerMREC)
            {
                Debug.Log("MREC-Banner".Ads("Hide Banner because SwitchBannerMREC = true [Show]"));
                ApplovinManager.BannerModule.HideBanner();
            }
        }
    }

    public void ResumeMREC()
    {
        var adsConfig = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
        if (ApplovinManager.ActiveMREC && adsConfig.ShowMREC)
        {
            Debug.Log("MREC".Ads("Resume"));
            MaxSdk.ShowMRec(mrecId);
            MaxSdkMREC.ShowMRec(mrecId, ApplovinManager.AdConstants.mrecPosition);
        }
    }

    public void HideMREC(bool isTemporary = false)
    {
        if (!isTemporary) ApplovinManager.ActiveMREC = false;
        Debug.Log("MREC".Ads($"Ad Hide: {(isTemporary ? "AOA resume" : "Close")}"));
        MaxSdk.HideMRec(mrecId);
        MaxSdkMREC.HideMREC();
        if (!isTemporary && RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).SwitchBannerMREC)
        {
            Debug.Log("MREC-Banner".Ads("Show Banner because MREC is closed and SwitchBannerMREC = true"));
            ApplovinManager.BannerModule.ShowBanner();
        }
    }

    private void OnMRECAdLoadedEvent(string adUnitId, AdInfo adInfo)
    {
        Debug.Log("MREC".Ads($"Ad Loaded"));
    }

    private void OnMRECAdLoadFailedEvent(string adUnitId, ErrorInfo errorInfo)
    {
        Debug.Log("MREC".Ads($"Ad Load Failed: {errorInfo.Message}"));
    }

    private void OnMRECAdClickedEvent(string adUnitId, AdInfo adInfo)
    {
        AdAnalytic.LogAd("show_mrec_ads_click", "");
    }

    private void OnMRECAdExpandedEvent(string adUnitId, AdInfo adInfo) { }

    private void OnMRECAdCollapsedEvent(string adUnitId, AdInfo adInfo) { }

    #endregion

    #region RevenuePaidEvent
    private void OnAdRevenuePaidEvent(string adUnitId, AdInfo impressionData)
    {
        ApplovinManager.SendAdRevenueTracker(impressionData);
    }
    #endregion
}
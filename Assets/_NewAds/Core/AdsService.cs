using System;
using System.Collections;
using UnityEngine;

public class AdsService : MonoBehaviour
{
    public static AdsService Instance { get; private set; }
    public AdsRemoteConfig Config { get; private set; }
    public IAdsTracker Tracker { get; private set; }
    public IAdsAnalytics Analytics { get; private set; }

    public BannerAdModule Banner { get; private set; }
    public InterstitialAdModule Interstitial { get; private set; }
    public RewardedAdModule Rewarded { get; private set; }
    public AppOpenAdModule AppOpen { get; private set; }

    private void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // 1. Load remote config (giả lập async, có thể thay bằng Firebase RemoteConfig, v.v.)
        yield return StartCoroutine(RemoteConfigLoader.Load((config) => {
            Config = config;
        }));

        // 2. Khởi tạo tracker và analytics (có thể chọn Adjust, AppsFlyer, Firebase, ...)
        Tracker = new AppsFlyerTracker(); // hoặc AdjustTracker
        Analytics = new UnityAnalytics(); // hoặc FirebaseAnalytics

        // 3. Khởi tạo các module quảng cáo
        Banner = new BannerAdModule(Config, Tracker, Analytics);
        Interstitial = new InterstitialAdModule(Config, Tracker, Analytics);
        Rewarded = new RewardedAdModule(Config, Tracker, Analytics);
        AppOpen = new AppOpenAdModule(Config, Tracker, Analytics);

        // 4. Bắt đầu load các quảng cáo cần thiết
        Banner.Preload();
        Interstitial.Preload();
        Rewarded.Preload();
        AppOpen.Preload();
    }
}

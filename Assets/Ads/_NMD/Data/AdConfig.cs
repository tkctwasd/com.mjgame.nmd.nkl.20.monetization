namespace A
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Ads/AdsConfig", fileName = "AdsConfig")]
    public class AdConfigSO : ScriptableObject
    {

    }

    [Serializable]
    public class AdsConfig
    {
        public bool ShowInfo = true;
        public bool ShowAppOpen = true;
        public int IntervalShowAppOpen = 60;
        public bool ShowInterstitial = true;
        public bool ShowBanner = true;
    }

    public static class RemoteKey
    {
        public static string AdUseAppOpen = "Ad_Use_AppOpen";
        public static string AdIntervalShowAppOpen = "Ad_Interval_Show_AppOpen";
        public static string AdUseInterstitial = "Ad_Use_Interstitial";
        public static string AdUseBanner = "Ad_Use_Banner";
    }
}
namespace A
{
    using System;
    [Serializable]
    public class AdsConfig
    {
        public bool ShowInfo = true;
        public bool ShowAppOpen = true;
        public bool ShowInterstitial = true;
        public bool ShowBanner = true;
        public int InterstitialCappingTime = 45;
        public int AppOpenCappingTime = 60;
    }

    public static class RemoteKey
    {
        public static string AdsConfig = "AdsConfig";
    }
}
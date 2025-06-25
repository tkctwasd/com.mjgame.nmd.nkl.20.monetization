namespace A
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AdConstants", menuName = "Ads/Ad Constants")]
    public class AdConstants : ScriptableObject
    {
        [Header("MAX SDK Keys")]
        public string SdkKey = "sdk-key";
        public string AdmobAndroidAppId = "admob-android-id";
        public string AdmobIosAppId = "admob-ios-id";

        [Header("App Information")]
        public string GameName = "Your Game Name";
        public string PackageName = "com.yourcompany.yourgame";
        public string PrivacyPolicyURL = "https://yourcompany.com/privacy";
        public string TermsURL = "https://yourcompany.com/terms";

        [Header("Ad Unit ID")]
        public string AppOpenId = "app-open-id";
        public string BannerId = "banner-id";
        public string InterstitialId = "interstitial-id";
        public string RewardedId = "rewarded-id";

        [Header("Banner")]
        public MaxSdkBase.AdViewPosition bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;

    }
}
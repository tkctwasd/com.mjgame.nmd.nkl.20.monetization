#if UNITY_EDITOR
using AppLovinMax.Scripts.IntegrationManager.Editor;
using UnityEditor;
#endif

using UnityEngine;

[CreateAssetMenu(fileName = "AdConstants", menuName = "Ads/Ad Constants")]
public class AdConstants : ScriptableObject
{
    [Header("MAX SDK Keys")]
    [ColorLabel(1f, 0.65f, 0f)] public string SdkKey = "sdk-key";

    [Header("Admob")]
    [ColorLabel] public string AdmobAndroidAppId = "admob-android-id";
    [ColorLabel(0f, 0.86f, 1f)] public string AdmobIosAppId = "admob-ios-id";

    [Header("App Information")]
    [ColorLabel(1f, 0.65f, 0f)] public string GameName = "Your Game Name";
    [ColorLabel(1f, 0.65f, 0f)] public string PackageName = "com.yourcompany.yourgame";
    [ColorLabel(1f, 0.65f, 0f)] public string PrivacyPolicyURL = "https://yourcompany.com/privacy";
    [ColorLabel(1f, 0.65f, 0f)] public string TermsURL = "https://yourcompany.com/terms";

    [Header("Ad Unit ID")]
    [ColorLabel(1f, 0.65f, 0f)] public string AppOpenId = "app-open-id";
    [ColorLabel(1f, 0.65f, 0f)] public string BannerId = "banner-id";
    [ColorLabel(1f, 0.65f, 0f)] public string InterstitialId = "interstitial-id";
    [ColorLabel(1f, 0.65f, 0f)] public string RewardedId = "rewarded-id";

    [Header("Banner")]
    public MaxSdkBase.AdViewPosition bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;


#if UNITY_EDITOR
    [Button(20)]
    private void ApplyAdIds()
    {
        AppLovinSettings.Instance.SdkKey = SdkKey;
        AppLovinInternalSettings.Instance.ConsentFlowEnabled = true;
        AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = PrivacyPolicyURL;
        AppLovinInternalSettings.Instance.ConsentFlowTermsOfServiceUrl = TermsURL;

        PlayerSettings.productName = GameName;
        PlayerSettings.applicationIdentifier = PackageName;

#if UNITY_ANDROID
        AppLovinSettings.Instance.AdMobAndroidAppId = AdmobAndroidAppId;
#elif UNITY_IOS
        AppLovinSettings.Instance.AdMobIosAppId = AdmobIosAppId;
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green><b> Ad info applied successfully!</b></color>");
    }
#endif

}
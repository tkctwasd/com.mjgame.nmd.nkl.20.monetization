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
    [ColorLabel(1f, 0.65f, 0f)] public string MRECId = "mrec-id";
    [ColorLabel(1f, 0.65f, 0f)] public string InterstitialIdHigh = "interstitial-high-id";
    [ColorLabel(1f, 0.65f, 0f)] public string InterstitialIdMedium = "interstitial-medium-id";
    [ColorLabel(1f, 0.65f, 0f)] public string InterstitialIdMain = "interstitial-main-id";
    [ColorLabel(1f, 0.65f, 0f)] public string RewardedIdHigh = "rewarded-high-id";
    [ColorLabel(1f, 0.65f, 0f)] public string RewardedIdMedium = "rewarded-medium-id";
    [ColorLabel(1f, 0.65f, 0f)] public string RewardedIdMain = "rewarded-main-id";

    [Header("Banner")]
    public MaxSdkBase.AdViewPosition bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;

    [Header("MREC")]
    public MaxSdkBase.AdViewPosition mrecPosition = MaxSdkBase.AdViewPosition.TopLeft;

    [Header("Adjust")]
    [ColorLabel(0.185f, 0.65f, 1f)] public string adjust = "id_adjust";
    
    [Header("Appsflyer")]
    [ColorLabel(0.185f, 0.96f, 1)] public string appsflyer = "id-appsflyer";
    [ColorLabel(0.185f, 0.96f, 1)] public string devKey = "dev-key-appsflyer";


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
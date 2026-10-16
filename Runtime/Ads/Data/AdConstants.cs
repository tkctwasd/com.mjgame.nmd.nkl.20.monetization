#if UNITY_EDITOR && USE_APPLOVIN
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
    [ColorLabel(1f, 0.65f, 0f)] public string InterstitialId = "interstitial-id";
    [ColorLabel(1f, 0.65f, 0f)] public string BannerId = "banner-id";
    [ColorLabel(1f, 0.65f, 0f)] public string RewardedId = "rewarded-id";
    [ColorLabel(1f, 0.65f, 0f)] public string MRECId = "mrec-id";

#if USE_APPLOVIN
    [Header("Banner")]
    public MaxSdkBase.AdViewPosition bannerPosition = MaxSdkBase.AdViewPosition.BottomCenter;

    [Header("MREC")]
    public MaxSdkBase.AdViewPosition mrecPosition = MaxSdkBase.AdViewPosition.TopLeft;
#endif


#if UNITY_EDITOR && USE_APPLOVIN
    [Button(20)]
    private void ApplyAdIds()
    {
        if (EditorPrefs.GetBool("Monetization.SetInfo.HasUnsavedDraft", false))
        {
            Debug.LogWarning("[Ads] Apply cancelled: save the Set Info draft before applying MAX settings.", this);
            EditorUtility.DisplayDialog("Apply ID Ads", "Hay Save thay doi trong tab Set Info truoc khi Apply Ad Ids.", "OK");
            return;
        }
        var maxSettings = AppLovinSettings.Instance;
        if (maxSettings == null)
        {
            Debug.LogError("[Ads] AppLovinSettings was not found. Apply cancelled.", this);
            return;
        }

        Undo.RecordObject(maxSettings, "Apply MAX Ad Settings");
        maxSettings.SdkKey = SdkKey;
        // Keep both platform IDs in MAX settings so switching the build target does not
        // leave the other platform with an old or empty App ID.
        maxSettings.AdMobAndroidAppId = AdmobAndroidAppId;
        maxSettings.AdMobIosAppId = AdmobIosAppId;
        maxSettings.SaveAsync();

        var internalSettings = AppLovinInternalSettings.Instance;
        if (internalSettings != null)
        {
            Undo.RecordObject(internalSettings, "Apply MAX Privacy Settings");
            internalSettings.ConsentFlowEnabled = true;
            internalSettings.ConsentFlowPrivacyPolicyUrl = PrivacyPolicyURL;
            internalSettings.ConsentFlowTermsOfServiceUrl = TermsURL;
            internalSettings.Save();
        }

        PlayerSettings.productName = GameName;
        PlayerSettings.applicationIdentifier = PackageName;

        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green><b>MAX settings applied and saved.</b></color> Settings asset: {AssetDatabase.GetAssetPath(maxSettings)}", this);
    }
#endif

}

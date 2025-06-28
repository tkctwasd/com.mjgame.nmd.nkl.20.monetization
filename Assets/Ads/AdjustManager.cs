#if USE_ADJUST
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdjustSdk;

public class AdjustManager : MonoBehaviour
{
    public static AdjustManager Instance;
    [SerializeField] string adjust = "id_adjust";
    private void Awake()
    {
        Instance = this;
    }
    public void Init()
    {
        LogMessage.Log("Adjust SetInfo", "Ads");
#if UNITY_ANDROID && UNITY_2019_2_OR_NEWER
        Application.deepLinkActivated += (deeplink) =>
        {
            Adjust.ProcessDeeplink(new AdjustDeeplink(deeplink));
        };
        if(!string.IsNullOrEmpty(Application.absoluteURL))
        {
            Adjust.ProcessDeeplink(new AdjustDeeplink(Application.absoluteURL));
        }
#endif
        AdjustConfig adjustConfig = new AdjustConfig(adjust, AdjustEnvironment.Production,
          false);
        adjustConfig.LogLevel = AdjustLogLevel.Info;
        adjustConfig.IsSendingInBackgroundEnabled = false;
        adjustConfig.IsDeferredDeeplinkOpeningEnabled = true;
        adjustConfig.IsCoppaComplianceEnabled = false;
        adjustConfig.IsCostDataInAttributionEnabled = false;
        adjustConfig.IsPreinstallTrackingEnabled = false;
        adjustConfig.IsPlayStoreKidsComplianceEnabled = false;
        adjustConfig.IsAdServicesEnabled = true;
        adjustConfig.IsIdfaReadingEnabled = true;
        adjustConfig.IsLinkMeEnabled = false;
        adjustConfig.IsSkanAttributionEnabled = true;
        Adjust.InitSdk(adjustConfig);
    }
    public string GetTokenAdjust()
    {
        return adjust;
    }    
}
#endif
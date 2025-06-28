#if USE_APPSFLYER
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

public class AppsFlyerManager : MonoBehaviour, IAppsFlyerConversionData
{
    public static AppsFlyerManager Instance;
    public string devKey = "dev-key-appsflyer";

    [Header("iOS")]
    public string appID = "id-app";

    private void Awake()
    {
        Instance = this;
    }
    public void Init()
    {
        LogMessage.Log("Appsflyer Init", "Ads");
#if UNITY_ANDROID
        AppsFlyer.initSDK(devKey, Application.identifier, this);
#elif UNITY_IOS
        AppsFlyer.initSDK(devKey, appID, this);
        if (appID.Equals(""))
            Debug.LogError("AppId is empty");
#endif
        AppsFlyer.startSDK();
        AppsFlyer.enableTCFDataCollection(true);
    }
    public string GetDevKeyAndAppId()
    {
        return $"Dev Key: {devKey}, App ID: {appID}";
    }
    public void onConversionDataSuccess(string conversionData)
    {
        AppsFlyer.AFLog("onConversionDataSuccess", conversionData);
        Dictionary<string, object> conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);
    }

    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("onConversionDataFail", error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }
}
#endif
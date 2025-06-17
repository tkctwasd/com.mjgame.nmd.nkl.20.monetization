using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirebaseModules.RemoteConfig;
using FirebaseModules.Analytics;
using System;
public class Etc : MonoBehaviour
{

    #region Firebase_RemoteConfig 
    public int time;
    public bool isReal;
    public new string name;
    public AdsSettings AdsSettings;

    [ContextMenu("FirebaseRemoteConfig")]
    public void FirebaseRemoteConfig()
    {
        AdsSettings = RemoteConfigService.Model<AdsSettings>("ads_settings");
        time = RemoteConfigService.Value<int>("time");
        isReal = RemoteConfigService.Value<bool>("isReal");
        name = RemoteConfigService.Value<string>("name");
    }

    #endregion

    #region Firebase_Analytic
    [ContextMenu("FirebaseAnalytic")]
    public void FirebaseAnalytic()
    {
        FirebaseAnalyticsService.LogEvent("checkpoint_new");
        FirebaseAnalyticsService.LogEvent("checkpoint_continue", new Dictionary<string, object> {
        { "test_mode", true },
        { "time", DateTime.UtcNow.ToString("o") }
    });
    }
    #endregion
}

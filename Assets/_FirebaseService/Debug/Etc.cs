using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FirebaseModules.RemoteConfig;
public class Etc : MonoBehaviour
{
    public int time;
    public bool isReal;
    public new string name;
    public AdsSettings AdsSettings;

    [ContextMenu("Test")]
    public void T()
    {
        AdsSettings = RemoteConfigService.Model<AdsSettings>("ads_settings");
        time = RemoteConfigService.Value<int>("time");
        isReal = RemoteConfigService.Value<bool>("isReal");
        name = RemoteConfigService.Value<string>("name");
    }
}

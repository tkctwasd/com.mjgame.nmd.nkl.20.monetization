#if UNITY_EDITOR && USE_APPLOVIN
using System.Collections.Generic;
using System.Linq;
using AppLovinMax.Scripts.IntegrationManager.Editor;
using UnityEditor;
using UnityEngine;

public partial class ProjectInfoEditor
{
    private static PluginData pluginData;
    private static bool pluginDataLoading;

    partial void EnsurePluginData()
    {
        if (pluginData == null) LoadPluginData();
    }

    partial void LoadPluginData()
    {
        if (pluginDataLoading) return;
        pluginDataLoading = true;
        AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(data =>
        {
            pluginDataLoading = false;
            if (data != null) pluginData = data;
            Collect();
        }));
    }

    partial void DrawAppLovinToolbar()
    {
        if (GUILayout.Button("AdConstants", EditorStyles.toolbarButton)) SelectAdConstants();
        if (GUILayout.Button("MAX Manager", EditorStyles.toolbarButton)) OpenMaxManager();
    }

    static partial void OnUpdateRowClicked() => OpenMaxManager();

    partial void CollectAppLovinDetails(List<Section> list) => list.Add(CollectAppLovin());

    partial void CollectAppLovinMarketing(List<Section> list)
    {
        list.Add(CollectMarketingAdIds());
        list.Add(CollectMarketingMaxPlugin());
        list.Add(CollectMarketingNetworks());
        list.Add(CollectMarketingMaxSettings());
    }

    private static void OpenMaxManager() => EditorApplication.ExecuteMenuItem("AppLovin/Integration Manager");

    private static Section CollectMarketingAdIds()
    {
        var ad = FindAsset<AdConstants>();
        var s = new Section { Title = ad != null ? $"Ad IDs ({ad.name})" : "Ad IDs (AdConstants not found)" };
        s.Add("Interstitial", ad != null ? ad.InterstitialId : null);
        s.Add("Rewarded", ad != null ? ad.RewardedId : null);
        s.Add("MREC", ad != null ? ad.MRECId : null);
        s.Add("App Open (AOA)", ad != null ? ad.AppOpenId : null);
        s.Add("Banner", ad != null ? ad.BannerId : null);

        // Chi hien MMP dang bat; neu chua bat cai nao thi hien ca hai de biet dang thieu
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
        var useAdjust = defines.Contains("USE_ADJUST");
        var useAppsFlyer = defines.Contains("USE_APPSFLYER");
        if (!useAdjust && !useAppsFlyer) useAdjust = useAppsFlyer = true;
        if (useAdjust) s.Add("Adjust Token", FindSerializedValue("AdjustManager", "adjust"));
        if (useAppsFlyer)
        {
            s.Add("AppsFlyer Dev Key", FindSerializedValue("AppsFlyerManager", "devKey"));
            s.Add("AppsFlyer App ID", FindSerializedValue("AppsFlyerManager", "appID"));
        }
        return s;
    }

    private static Section CollectMarketingMaxPlugin()
    {
        var s = new Section { Title = "AppLovin MAX Plugin Details", Header = new[] { "Platform", "Current Version" } };
        GetMaxNativeVersions(out var android, out var ios);
        var latest = pluginData?.AppLovinMax?.LatestVersions;
        AddVersionRow(s, "Unity 3D", MaxSdk.Version, latest?.Unity);
        AddVersionRow(s, "Android", android, latest?.Android);
        AddVersionRow(s, "iOS", ios, latest?.Ios);
        return s;
    }

    private static void AddVersionRow(Section s, string key, string current, string latest)
    {
        if (!IsMissing(current) && !string.IsNullOrEmpty(latest)
            && MaxSdkUtils.CompareVersions(current, latest) == MaxSdkUtils.VersionComparisonResult.Lesser)
            s.AddUpdate($"Update → {latest}", key, current);
        else s.Add(key, current);
    }

    private static Section CollectMarketingNetworks()
    {
        var title = "Mediated Networks (Installed)";
        if (pluginDataLoading) title += " - checking updates...";
        else if (pluginData == null) title += " - offline, no update check";
        var s = new Section { Title = title, Header = new[] { "Network", "Android", "iOS" } };

        if (pluginData != null)
        {
            var installed = pluginData.MediatedNetworks
                .Where(n => n.CurrentVersions != null && !string.IsNullOrEmpty(n.CurrentVersions.Unity))
                .OrderBy(n => n.DisplayName);
            foreach (var n in installed)
            {
                var android = string.IsNullOrEmpty(n.CurrentVersions.Android) ? "-" : n.CurrentVersions.Android;
                var ios = string.IsNullOrEmpty(n.CurrentVersions.Ios) ? "-" : n.CurrentVersions.Ios;
                // Network chi co 1 nen tang (vd CSJ chi iOS) se co "-" nhung khong phai thieu
                var row = new Row { Cells = new[] { n.DisplayName, android, ios } };
                if (n.CurrentToLatestVersionComparisonResult == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    row.Status = Status.Update;
                    row.Note = $"Update → {n.LatestVersions.Unity}";
                }
                s.Rows.Add(row);
            }
        }
        else
        {
            foreach (var a in GetAdapters()) s.Rows.Add(new Row { Cells = new[] { a.Network, a.Android, a.Ios } });
        }

        if (s.Rows.Count == 0) s.Add("Networks", null, null);
        return s;
    }

    private static Section CollectMarketingMaxSettings()
    {
        var s = new Section { Title = "AppLovin SDK & Privacy Settings" };
        var settings = AppLovinSettings.Instance;
        var internalSettings = AppLovinInternalSettings.Instance;
        s.Add("AppLovin SDK Key", settings != null ? settings.SdkKey : null);
        s.Add("Google App ID (Android)", settings != null ? settings.AdMobAndroidAppId : null);
        s.Add("Google App ID (iOS)", settings != null ? settings.AdMobIosAppId : null);
        s.Add("MAX Ad Review", settings != null ? settings.QualityServiceEnabled.ToString() : null);
        s.Space();
        s.Add("Terms & Privacy Flow", internalSettings != null ? internalSettings.ConsentFlowEnabled.ToString() : null);
        s.Add("Privacy Policy URL", internalSettings != null ? internalSettings.ConsentFlowPrivacyPolicyUrl : null);
        s.Add("Terms of Service URL", internalSettings != null ? internalSettings.ConsentFlowTermsOfServiceUrl : null);
        return s;
    }

    private static Section CollectAppLovin()
    {
        var s = new Section { Title = "AppLovin MAX" };
        GetMaxNativeVersions(out var android, out var ios);
        s.Add("MAX Unity Plugin", MaxSdk.Version);
        s.Add("MAX Android SDK", android);
        s.Add("MAX iOS SDK", ios);

        var settings = AppLovinSettings.Instance;
        if (settings != null)
        {
            s.Add("SDK Key (Settings)", settings.SdkKey);
            s.Add("AdMob Android App ID", settings.AdMobAndroidAppId);
            s.Add("AdMob iOS App ID", settings.AdMobIosAppId);
            s.Add("Quality Service", settings.QualityServiceEnabled.ToString());
        }

        var adConstants = FindAsset<AdConstants>();
        if (adConstants != null)
        {
            s.Add("AdConstants Asset", AssetDatabase.GetAssetPath(adConstants));
            s.Add("App Open ID", adConstants.AppOpenId);
            s.Add("Banner ID", adConstants.BannerId);
            s.Add("MREC ID", adConstants.MRECId);
            s.Add("Interstitial ID", adConstants.InterstitialId);
            s.Add("Rewarded ID", adConstants.RewardedId);
        }
        else s.Add("AdConstants Asset", "Not found");

        foreach (var a in GetAdapters()) s.Add($"Adapter {a.Network}", $"Android {a.Android} | iOS {a.Ios}");
        return s;
    }

    private static void SelectAdConstants()
    {
        var ad = FindAsset<AdConstants>();
        if (ad == null) return;
        Selection.activeObject = ad;
        EditorGUIUtility.PingObject(ad);
    }
}
#endif

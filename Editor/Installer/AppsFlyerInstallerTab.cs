#if UNITY_EDITOR
using UnityEditor;

// AppsFlyer: appsflyer-unity-plugin-<ver>.unitypackage (bo ban strict-mode), tag v7.0.3
public class AppsFlyerInstallerTab : SdkInstallerTab
{
    public AppsFlyerInstallerTab(EditorWindow host) : base(host, "AppsFlyer", "AppsFlyerSDK/appsflyer-unity-plugin", "USE_APPSFLYER",
        tag => tag.TrimStart('v'),
        asset => asset.EndsWith(".unitypackage") && !asset.Contains("strict"),
        () => FindVersionInScript("AppsFlyer", @"kAppsFlyerPluginVersion\s*=\s*""([^""]+)"""),
        "Assets/AppsFlyer")
    {
    }
}
#endif

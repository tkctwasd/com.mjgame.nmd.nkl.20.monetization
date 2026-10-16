#if UNITY_EDITOR
using UnityEditor;

// AppLovin MAX: AppLovin-MAX-Unity-Plugin-<ver>-Android-..-iOS-...unitypackage, tag release_8_6_6
public class AppLovinInstallerTab : SdkInstallerTab
{
    public AppLovinInstallerTab(EditorWindow host) : base(host, "AppLovin", "AppLovin/AppLovin-MAX-Unity-Plugin", "USE_APPLOVIN",
        tag => tag.Replace("release_", "").Replace('_', '.'),
        asset => asset.EndsWith(".unitypackage"),
        () => FindVersionInScript("MaxSdk", @"_version\s*=\s*""([\d\.]+)"""),
        "Assets/MaxSdk")
    {
        // Adapter mediation va AppLovinSettings (SDK key) phai giu khi doi version
        KeepOnChange = new[] { "Assets/MaxSdk/Mediation/", "Assets/MaxSdk/Resources/" };
    }
}
#endif

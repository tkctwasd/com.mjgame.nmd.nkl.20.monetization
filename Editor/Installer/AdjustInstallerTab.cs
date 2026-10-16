#if UNITY_EDITOR
using UnityEditor;

// Adjust: Adjust_v<ver>.unitypackage, tag v5.8.1
public class AdjustInstallerTab : SdkInstallerTab
{
    public AdjustInstallerTab(EditorWindow host) : base(host, "Adjust", "adjust/unity_sdk", "USE_ADJUST",
        tag => tag.TrimStart('v'),
        asset => asset.EndsWith(".unitypackage"),
        () => FindVersionInFile("Assets/Adjust/package.json", @"""version""\s*:\s*""([^""]+)"""),
        "Assets/Adjust")
    {
    }
}
#endif

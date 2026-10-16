#if UNITY_EDITOR
using UnityEditor;

// Google Play In-App Review: com.google.play.review-<ver>.unitypackage, tag v1.8.4
public class ReviewInstallerTab : SdkInstallerTab
{
    public ReviewInstallerTab(EditorWindow host) : base(host, "Review", "google/play-in-app-reviews-unity", "USE_GOOGLE_REVIEW",
        tag => tag.TrimStart('v'),
        asset => asset.EndsWith(".unitypackage"),
        () => FindVersionInFile("Assets/GooglePlayPlugins/com.google.play.review/package.json", @"""version""\s*:\s*""([^""]+)"""),
        "Assets/GooglePlayPlugins/com.google.play.review")
    {
    }
}
#endif

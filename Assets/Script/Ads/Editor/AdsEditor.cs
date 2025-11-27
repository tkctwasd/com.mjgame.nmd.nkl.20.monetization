#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AdsSymbolEditor
{

    [MenuItem("Tools/Ads/Toggle USE_ADJUST")]
    public static void ToggleAdjust() => ToggleSymbol("USE_ADJUST");

    [MenuItem("Tools/Ads/Toggle USE_FIREBASE")]
    public static void ToggleFirebaseRC() => ToggleSymbol("FIREBASE_ANALYTIC");

    [MenuItem("Tools/Ads/Toggle USE_APPSFLYER")]
    public static void ToggleAppsFlyer() => ToggleSymbol("USE_APPSFLYER");

    private static void ToggleSymbol(string symbol)
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').ToList();

        if (defines.Contains(symbol)) defines.Remove(symbol);
        else defines.Add(symbol);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        Debug.Log($"Toggled {symbol}, new define symbols: {string.Join(";", defines)}");
    }
}
#endif

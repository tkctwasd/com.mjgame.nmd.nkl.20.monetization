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


[CustomEditor(typeof(AdsManager))]
public class AdsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AdsManager adsManager = (AdsManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Ads", EditorStyles.boldLabel);

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("Resume (2s)", GUILayout.Height(30)))
        {
            adsManager.StartCoroutine(SimulatePauseRoutineIE(adsManager));
        }
    }

    private System.Collections.IEnumerator SimulatePauseRoutineIE(AdsManager adsManager)
    {
        Debug.Log("<color=yellow>Editor: Simulating App Pause...</color>");

        adsManager.SendMessage("OnApplicationPause", true);

        yield return new WaitForSecondsRealtime(2f);

        Debug.Log("<color=green>Editor: Simulating App Resume...</color>");
        adsManager.SendMessage("OnApplicationPause", false);
    }
}
#endif
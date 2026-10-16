#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AdConstantsAutoCreate
{
    const string Folder = "Assets/Resources", Path = Folder + "/AdConstants.asset";

    static AdConstantsAutoCreate() => EditorApplication.delayCall += () => GetOrCreate();

    public static AdConstants GetOrCreate()
    {
        var guids = AssetDatabase.FindAssets("t:AdConstants");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<AdConstants>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (EditorApplication.isPlayingOrWillChangePlaymode) return null;

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Resources");
        var asset = ScriptableObject.CreateInstance<AdConstants>();
        AssetDatabase.CreateAsset(asset, Path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Ads] Created {Path}");
        return asset;
    }
}

#if USE_APPLOVIN
[CustomEditor(typeof(AdsManager))]
public class AdsManagerEditor : Editor
{
    void OnEnable()
    {
        var manager = (AdsManager)target;
        if (manager.AdConstants != null) return;
        var asset = AdConstantsAutoCreate.GetOrCreate();
        if (asset == null) return;
        Undo.RecordObject(manager, "Assign AdConstants");
        manager.AdConstants = asset;
        EditorUtility.SetDirty(manager);
    }

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
#endif

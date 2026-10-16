#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Docked beside Ads Installer. Deliberately has no MenuItem: it is opened by Ads Installer.
public class SetInfoWindow : EditorWindow
{
    const string DirtyStateKey = "Monetization.SetInfo.HasUnsavedDraft";
    UnityEditor.Editor draftEditor;
    AdConstants asset, draft;
    bool dirty;
    Vector2 scroll;
    string savedReport, pendingDiff = "";

    void OnEnable()
    {
        titleContent = AdsInstallerWindow.Title("2. Set Info", "d_Settings");
    }

    void OnDestroy()
    {
        if (dirty && draft != null && asset != null &&
            EditorUtility.DisplayDialog("Set Info", "Co thay doi chua Save. Luu vao AdConstants?", "Save", "Bo"))
            Save();
        EditorPrefs.SetBool(DirtyStateKey, false);
        if (draft != null) DestroyImmediate(draft);
    }

    void OnDisable()
    {
        if (draftEditor != null) DestroyImmediate(draftEditor);
    }

    void LoadDraft()
    {
        if (draft == null)
        {
            draft = CreateInstance<AdConstants>();
            draft.hideFlags = HideFlags.DontSave;
        }
        EditorUtility.CopySerialized(asset, draft);
        dirty = false;
        EditorPrefs.SetBool(DirtyStateKey, false);
    }

    string Diff()
    {
        var a = new SerializedObject(asset);
        var b = new SerializedObject(draft);
        var lines = new System.Collections.Generic.List<string>();
        var it = b.GetIterator();
        for (var enter = true; it.NextVisible(enter); enter = false)
        {
            if (it.propertyPath == "m_Script") continue;
            var old = a.FindProperty(it.propertyPath);
            if (old == null || SerializedProperty.DataEquals(old, it)) continue;
            lines.Add($"{it.displayName}: {Value(old)} -> {Value(it)}");
        }
        return string.Join("\n", lines);
    }

    static string Value(SerializedProperty p) => p.propertyType switch
    {
        SerializedPropertyType.String => string.IsNullOrEmpty(p.stringValue) ? "(trong)" : p.stringValue,
        SerializedPropertyType.Enum => p.enumDisplayNames.Length > p.enumValueIndex && p.enumValueIndex >= 0 ? p.enumDisplayNames[p.enumValueIndex] : p.intValue.ToString(),
        SerializedPropertyType.Boolean => p.boolValue.ToString(),
        SerializedPropertyType.Integer => p.intValue.ToString(),
        SerializedPropertyType.Float => p.floatValue.ToString(),
        _ => p.propertyType.ToString()
    };

    void Save()
    {
        var diff = Diff();
        savedReport = diff.Length > 0 ? $"Da Save:\n{diff}" : "Da Save (khong co gi thay doi)";
        Debug.Log("[Set Info] " + savedReport);
        Undo.RecordObject(asset, "Set Info");
        EditorUtility.CopySerialized(draft, asset);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        dirty = false;
        EditorPrefs.SetBool(DirtyStateKey, false);
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    void OnGUI()
    {
        if (asset == null) asset = AdConstantsAutoCreate.GetOrCreate();
        if (asset == null)
        {
            EditorGUILayout.HelpBox("Chua tao duoc AdConstants (dang Play Mode?).", MessageType.Warning);
            return;
        }
        if (draft == null) LoadDraft();
        UnityEditor.Editor.CreateCachedEditor(draft, null, ref draftEditor);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset), EditorStyles.miniLabel);
        if (GUILayout.Button("Select", GUILayout.Width(60))) EditorGUIUtility.PingObject(Selection.activeObject = asset);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUI.BeginChangeCheck();
        draftEditor.OnInspectorGUI();
        if (EditorGUI.EndChangeCheck())
        {
            dirty = true;
            EditorPrefs.SetBool(DirtyStateKey, true);
            savedReport = null;
            pendingDiff = Diff();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (savedReport != null)
        {
            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            EditorGUILayout.HelpBox(savedReport, MessageType.Info);
            GUI.backgroundColor = Color.white;
        }
        if (dirty)
        {
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            EditorGUILayout.HelpBox(pendingDiff.Length > 0 ? $"Chua Save, se thay doi:\n{pendingDiff}" : "Chua Save (giong asset, khong co gi thay doi)", MessageType.Warning);
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = dirty;
        if (GUILayout.Button("Revert", GUILayout.Height(28))) LoadDraft();
        GUI.backgroundColor = dirty ? new Color(0.4f, 1f, 0.4f) : Color.white;
        if (GUILayout.Button("Save", GUILayout.Height(28)))
        {
            Debug.Log("[Set Info] Se Save:\n" + pendingDiff);
            if (EditorUtility.DisplayDialog("Set Info - Xac nhan Save", pendingDiff.Length > 0 ? pendingDiff : "Khong co gi thay doi", "Save", "Huy")) Save();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }
}
#endif

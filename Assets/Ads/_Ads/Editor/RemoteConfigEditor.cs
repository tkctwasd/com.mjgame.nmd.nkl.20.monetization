using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RemoteConfigEditor : EditorWindow
{
    private static string folderPath = "Assets/Resources";
    private static string filePathRemoteConfig = "Assets/Resources/RemoteConfig_".PlatformName();
    private static Dictionary<string, object> remoteConfigData = new Dictionary<string, object>();
    private static Vector2 scrollPos;

    private void OnEnable()
    {
        LoadRemoteConfigFile(filePathRemoteConfig, ref remoteConfigData);
    }
    private void OnDisable()
    {
        SaveRemoteConfigFile(filePathRemoteConfig, remoteConfigData);
    }
    [MenuItem("Tools/Ads/RemoteConfig Editor")]
    public static void ShowWindow()
    {
        RemoteConfigEditor window = (RemoteConfigEditor)GetWindow(typeof(RemoteConfigEditor), true, "RemoteConfig Editor");
        Vector2 fixedSize = new Vector2(1000, 1000);
        window.minSize = fixedSize;
        window.maxSize = fixedSize;

        Rect mainEditorWindow = EditorGUIUtility.GetMainWindowPosition();
        float centerX = mainEditorWindow.x + (mainEditorWindow.width - fixedSize.x) / 2;
        float centerY = mainEditorWindow.y + (mainEditorWindow.height - fixedSize.y) / 2;

        window.position = new Rect(centerX, centerY, fixedSize.x, fixedSize.y);
    }

    private void OnGUI()
    {
        if(!string.IsNullOrEmpty(filePathRemoteConfig))
        {
            GUILayout.Label("File Remote Config:", EditorStyles.label);
            GUILayout.TextField(filePathRemoteConfig, GUILayout.ExpandWidth(true));
        }

        if(GUILayout.Button("Reload RemoteConfig"))
        {
            LoadRemoteConfigFile(filePathRemoteConfig, ref remoteConfigData);
        }

        GUILayout.Space(10);

        GUILayout.Label("Remote Config Data:", EditorStyles.boldLabel);
        DrawJsonEditor(ref remoteConfigData);

        if(GUILayout.Button("Remove All Remote Config"))
        {
            //ClearRemoteconfigData();
        }
    }

    private static void DrawJsonEditor(ref Dictionary<string, object> jsonData)
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(800));

        List<string> keysToRemove = new List<string>();
        Dictionary<string, object> updatedEntries = new Dictionary<string, object>(jsonData);

        string lastFocusedControl = GUI.GetNameOfFocusedControl();

        foreach(var key in new List<string>(jsonData.Keys))
        {
            EditorGUILayout.BeginHorizontal();

            string newKey = EditorGUILayout.TextField(key, GUILayout.Width(200));

            object newValue = jsonData[key];
            if(jsonData[key] is string)
            {
                newValue = EditorGUILayout.TextField(jsonData[key].ToString());
            }
            else if(jsonData[key] is int)
            {
                newValue = EditorGUILayout.IntField((int)jsonData[key]);
            }
            else if(jsonData[key] is float)
            {
                newValue = EditorGUILayout.FloatField((float)jsonData[key]);
            }
            else if(jsonData[key] is bool)
            {
                newValue = EditorGUILayout.Toggle((bool)jsonData[key]);
            }

            if(newKey != key)
            {
                if(updatedEntries.ContainsKey(newKey))
                {
                    EditorUtility.DisplayDialog("Duplicate Key", $"The key '{newKey}' already exists!", "OK");
                }
                else
                {
                    updatedEntries.Remove(key);
                    updatedEntries[newKey] = newValue;
                }
            }
            else
            {
                updatedEntries[key] = newValue;
            }

            if(GUILayout.Button("X", GUILayout.Width(20)))
            {
                keysToRemove.Add(key);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        foreach(var key in keysToRemove)
        {
            updatedEntries.Remove(key);
        }

        jsonData.Clear();
        foreach(var entry in updatedEntries)
        {
            jsonData[entry.Key] = entry.Value;
        }

        GUILayout.Space(5);

        if(GUILayout.Button("Add New Remote Config"))
        {
            int newIndex = jsonData.Count + 1;
            string newKey = "new_remote_config_key_" + newIndex;

            if(jsonData.ContainsKey(newKey))
            {
                EditorUtility.DisplayDialog("Duplicate Key", $"The key '{newKey}' already exists!", "OK");
            }
            else
            {
                jsonData[newKey] = "";
            }
        }
    }

    private static void LoadRemoteConfigFile(string path, ref Dictionary<string, object> jsonData)
    {
        if(!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(path);
            jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
        }
        catch(System.Exception ex)
        {
            Debug.LogError("Error read remoteconfig: " + ex.Message);
        }
    }
    private static void SaveRemoteConfigFile(string path, Dictionary<string, object> jsonData)
    {
        try
        {
            string formattedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(path, formattedJson);
            AssetDatabase.Refresh();
        }
        catch(System.Exception ex)
        {
            Debug.LogError("Error save remote config: " + ex.Message);
        }
    }

    [InitializeOnLoadMethod]
    private static void CreateRemoteConfig()
    {
        CreateRemoteConfigFile(filePathRemoteConfig);
    }

    private static void CreateRemoteConfigFile(string path)
    {
        if(File.Exists(path))
        {
            return;
        }

        if(!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllText(path, "{}");

        AssetDatabase.Refresh();
    }

    public static void ClearRemoteconfigData()
    {
        if(File.Exists(filePathRemoteConfig))
        {
            File.WriteAllText(filePathRemoteConfig, "{}");
            remoteConfigData.Clear();
        }
        AssetDatabase.Refresh();
    }
}
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseInstallerTab
{
    const string ReleasesApi = "https://api.github.com/repos/firebase/firebase-unity-sdk/releases?per_page=30";
    const string ZipUrl = "https://dl.google.com/firebase/sdk/unity/firebase_unity_sdk_{0}.zip";
    // Dung chung giua cac project: Windows D:\Advertisement, macOS ~/Advertisement; khong co o D thi luu tam trong Library
    internal static readonly string CacheDir =
        Application.platform == RuntimePlatform.OSXEditor ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Advertisement")
        : Directory.Exists(@"D:\") ? @"D:\Advertisement"
        : Path.Combine(Directory.GetCurrentDirectory(), "Library/FirebaseSdkCache");

    static readonly string[] Packages =
    {
        "FirebaseAnalytics", "FirebaseRemoteConfig", "FirebaseCrashlytics", "FirebaseMessaging", "FirebaseAuth",
        "FirebaseDatabase", "FirebaseFirestore", "FirebaseFunctions", "FirebaseStorage", "FirebaseInstallations",
        "FirebaseAppCheck", "FirebaseAI"
    };

    readonly List<string> versions = new List<string>();
    static readonly HashSet<string> Required = new HashSet<string> { "FirebaseAnalytics", "FirebaseRemoteConfig", "FirebaseCrashlytics" };

    readonly HashSet<string> selected = new HashSet<string>(Required);
    Dictionary<string, string> installed = new Dictionary<string, string>(), manifests = new Dictionary<string, string>();
    int versionIndex;
    bool enableSymbol = true;
    string status;
    UnityWebRequest request;

    readonly EditorWindow host;

    public FirebaseInstallerTab(EditorWindow host) => this.host = host;

    void Repaint() => host.Repaint();

    public void Enable()
    {
        installed = FindInstalled();
        // Chi goi GitHub 1 lan moi phien Editor, mo lai cua so dung danh sach da luu
        var cached = SessionState.GetString(VersionsKey, "");
        if (cached.Length == 0) FetchVersions();
        else
        {
            versions.Clear();
            versions.AddRange(cached.Split(';'));
            SelectDefaultVersion();
        }
    }

    public void Disable() => CancelRequest();

    public void Draw()
    {
        var busy = request != null;

        EditorGUILayout.LabelField("Installed", EditorStyles.boldLabel);
        if (installed.Count == 0) EditorGUILayout.LabelField("  Chua cai Firebase");
        foreach (var kv in installed) EditorGUILayout.LabelField($"  {kv.Key}", kv.Value);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, GUILayout.Width(60));
        GUI.enabled = !busy && versions.Count > 0;
        versionIndex = EditorGUILayout.Popup(versionIndex, versions.ToArray());
        GUI.enabled = !busy;
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) FetchVersions();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Packages", EditorStyles.boldLabel);
        if (GUILayout.Button("All", GUILayout.Width(50))) selected.UnionWith(Packages);
        if (GUILayout.Button("None", GUILayout.Width(50))) selected.Clear();
        EditorGUILayout.EndHorizontal();

        foreach (var p in Packages)
        {
            var required = Required.Contains(p);
            var label = p;
            if (required) label += "  [mac dinh - bat buoc]";
            if (installed.TryGetValue(p, out var v)) label += $"  (dang co {v})";
            var on = EditorGUILayout.ToggleLeft(label, selected.Contains(p));
            if (on) selected.Add(p); else selected.Remove(p);
        }

        enableSymbol = EditorGUILayout.ToggleLeft("Bat USE_FIREBASE sau khi import", enableSymbol);

        EditorGUILayout.Space();
        var conflict = VersionConflict(CurrentVersion);
        var current = installed.Values.FirstOrDefault();
        if (SdkInstallerTab.DrawLatest("Firebase", current, versions.Count > 0 ? versions[0] : null, CurrentVersion)) versionIndex = 0;
        if (conflict != null)
        {
            var up = SdkInstallerTab.CompareVersion(CurrentVersion, conflict) > 0;
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            EditorGUILayout.HelpBox($"Dang dung Firebase {conflict}. {(up ? "Update len" : "Ha xuong")} {CurrentVersion}?", MessageType.Warning);
            GUI.backgroundColor = Color.white;
            // Cac module cung version nen phai doi tat ca module dang cai
            var unticked = installed.Keys.Where(p => !selected.Contains(p)).ToList();
            if (unticked.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                EditorGUILayout.HelpBox($"Phai tick tat ca module dang cai moi doi duoc version.\nChua tick: {string.Join(", ", unticked)}", MessageType.Error);
                GUI.backgroundColor = Color.white;
            }
            GUI.enabled = !busy && unticked.Count == 0;
            if (GUILayout.Button($"{(up ? "Update len" : "Ha xuong")} {CurrentVersion}", GUILayout.Height(28))) ChangeVersion();
            GUI.enabled = true;
        }
        GUI.enabled = !busy && versions.Count > 0 && conflict == null && selected.Any(p => !IsInstalled(p, CurrentVersion));
        if (GUILayout.Button(IsCached(CurrentVersion) ? "Import" : "Download & Import", GUILayout.Height(30))) DownloadAndImport();
        GUI.enabled = !busy && selected.Any(p => installed.ContainsKey(p));
        if (GUILayout.Button("Remove selected from project", GUILayout.Height(24))) Remove(selected.Where(p => installed.ContainsKey(p)).ToList());
        GUI.enabled = !busy && installed.Count > 0;
        if (GUILayout.Button("Remove all Firebase", GUILayout.Height(24))) Remove(installed.Keys.ToList());
        GUI.enabled = true;

        if (busy)
        {
            var r = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(r, request.downloadProgress, $"Downloading {CurrentVersion}  {request.downloadProgress:P0}");
            if (GUILayout.Button("Cancel")) CancelRequest();
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        if (GUILayout.Button("Open cache folder", EditorStyles.miniButton))
        {
            Directory.CreateDirectory(CacheDir);
            EditorUtility.RevealInFinder(CacheDir);
        }
    }

    string CurrentVersion => versions.Count > 0 ? versions[versionIndex] : null;
    static string ZipPath(string version) => Path.Combine(CacheDir, $"firebase_unity_sdk_{version}.zip");
    bool IsInstalled(string package, string version) => installed.TryGetValue(package, out var v) && v == version;
    static string ExtractDir(string version) => Path.Combine(CacheDir, $"firebase_unity_sdk_{version}");
    static string PackagePath(string version, string package) => Path.Combine(ExtractDir(version), package + ".unitypackage");
    bool IsCached(string version) => version != null && (File.Exists(ZipPath(version))
        || selected.Where(p => !IsInstalled(p, version)).All(p => File.Exists(PackagePath(version, p))));

    const string VersionsKey = "FirebaseInstaller.Versions";

    // Uu tien version dang cai trong project, chua cai thi lay ban moi nhat
    void SelectDefaultVersion()
    {
        var current = installed.Values.FirstOrDefault();
        versionIndex = Mathf.Max(0, current != null ? versions.IndexOf(current) : 0);
    }

    void FetchVersions()
    {
        status = "Dang lay danh sach version...";
        request = UnityWebRequest.Get(ReleasesApi);
        request.SetRequestHeader("User-Agent", "Unity");
        var op = request.SendWebRequest();
        op.completed += _ =>
        {
            if (request == null) return;
            if (request.result != UnityWebRequest.Result.Success) status = $"Loi lay version: {request.error}";
            else
            {
                versions.Clear();
                foreach (Match m in Regex.Matches(request.downloadHandler.text, "\"tag_name\"\\s*:\\s*\"v?([^\"]+)\""))
                    versions.Add(m.Groups[1].Value);
                SessionState.SetString(VersionsKey, string.Join(";", versions));
                SelectDefaultVersion();
                status = versions.Count > 0 ? null : "Khong lay duoc version";
            }
            ClearRequest();
        };
    }

    string VersionConflict(string version) => installed.Values.FirstOrDefault(v => v != version);

    // Go tat ca module dang cai roi cai lai chung (kem module dang tick) o version dang chon
    void ChangeVersion()
    {
        var version = CurrentVersion;
        var from = installed.Values.First();
        var up = SdkInstallerTab.CompareVersion(version, from) > 0;
        if (installed.Keys.Any(p => !selected.Contains(p))) return;
        var modules = selected.ToList();
        var source = IsCached(version) ? $"Dung file co san trong {CacheDir}" : $"Tai firebase_unity_sdk_{version}.zip (~750MB) ve {CacheDir}";
        if (!EditorUtility.DisplayDialog($"{(up ? "Update" : "Ha version")} Firebase",
                $"Firebase: {from} -> {version}\n\n1. Xoa ban {from}: {string.Join(", ", installed.Keys)}\n2. Cai ban {version}:\n- {string.Join("\n- ", modules)}\n\n{source}",
                up ? "Update" : "Ha version", "Huy")) return;

        Remove(installed.Keys.ToList(), false);
        installed.Clear();
        DownloadAndImport(false);
    }

    void DownloadAndImport(bool confirm = true)
    {
        var version = CurrentVersion;
        if (VersionConflict(version) != null) return;
        var toImport = selected.Where(p => !IsInstalled(p, version)).ToList();
        var source = IsCached(version) ? $"Dung file co san trong {CacheDir}" : $"Tai firebase_unity_sdk_{version}.zip (~750MB) ve {CacheDir}";
        var symbolNote = enableSymbol ? "\nBat USE_FIREBASE" : "";
        if (confirm && !EditorUtility.DisplayDialog("Import Firebase",
                $"Cai Firebase {version}:\n- {string.Join("\n- ", toImport)}\n\n{source}{symbolNote}", "Cai", "Huy")) return;
        if (IsCached(version))
        {
            ImportFromZip(version);
            return;
        }

        Directory.CreateDirectory(CacheDir);
        var temp = ZipPath(version) + ".part";
        request = new UnityWebRequest(string.Format(ZipUrl, version), UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerFile(temp) { removeFileOnAbort = true }
        };
        status = $"Dang tai firebase_unity_sdk_{version}.zip (~750MB)...";
        EditorApplication.update += Repaint;
        var op = request.SendWebRequest();
        op.completed += _ =>
        {
            EditorApplication.update -= Repaint;
            if (request == null) return;
            var ok = request.result == UnityWebRequest.Result.Success;
            var error = request.error;
            ClearRequest();
            if (!ok)
            {
                status = $"Tai that bai: {error}";
                return;
            }
            File.Move(temp, ZipPath(version));
            ImportFromZip(version);
        };
    }

    void ImportFromZip(string version)
    {
        Directory.CreateDirectory(ExtractDir(version));
        var imported = new List<string>();
        var missing = new List<string>();
        var zipPath = ZipPath(version);
        var zip = File.Exists(zipPath) ? ZipFile.OpenRead(zipPath) : null;

        try
        {
            foreach (var name in selected)
            {
                if (IsInstalled(name, version)) continue;
                var target = PackagePath(version, name);
                if (!File.Exists(target))
                {
                    // Ban cu co ca dotnet3/dotnet4, uu tien dotnet4
                    var entry = zip?.Entries
                        .Where(e => e.Name == name + ".unitypackage")
                        .OrderBy(e => e.FullName.Contains("dotnet3") ? 1 : 0)
                        .FirstOrDefault();
                    if (entry == null)
                    {
                        missing.Add(name);
                        continue;
                    }
                    entry.ExtractToFile(target);
                }
                AssetDatabase.ImportPackage(target, false);
                imported.Add(name);
            }
        }
        finally
        {
            zip?.Dispose();
        }

        if (imported.Count > 0) AdsInstallerWindow.SetPending($"cai Firebase {version}: {string.Join(", ", imported)}");
        if (enableSymbol && imported.Count > 0) AddSymbol("USE_FIREBASE");
        status = $"Import {version}: {string.Join(", ", imported)}";
        if (missing.Count > 0) status += $"\nKhong co trong ban nay: {string.Join(", ", missing)}";
        installed = FindInstalled();
    }

    // Xoa theo manifest cua tung module; file dung chung (FirebaseApp...) chi xoa khi khong con module nao can
    void Remove(List<string> modules, bool confirm = true)
    {

        // Tinh truoc khi xoa vi AssetDatabase co the chua cap nhat manifest ngay sau DeleteAssets
        var removeAll = manifests.Keys.All(modules.Contains);
        var keep = new HashSet<string>(manifests.Where(kv => !modules.Contains(kv.Key)).SelectMany(kv => ReadManifest(kv.Value)));
        var paths = modules.SelectMany(m => ReadManifest(manifests[m]).Append(manifests[m]))
            .Distinct()
            .Where(p => !keep.Contains(p) && (File.Exists(p) || Directory.Exists(p)))
            .ToArray();

        var folders = paths.Select(TopFolder).Distinct();
        var symbolNote = removeAll ? "\nGo USE_FIREBASE" : "";
        if (confirm && !EditorUtility.DisplayDialog("Remove Firebase",
                $"Xoa khoi project:\n- {string.Join("\n- ", modules)}\n\n{paths.Length} file trong:\n{string.Join("\n", folders)}{symbolNote}", "Xoa", "Huy")) return;

        if (confirm) AdsInstallerWindow.SetPending($"xoa Firebase: {string.Join(", ", modules)}");
        var failed = new List<string>();
        AssetDatabase.DeleteAssets(paths, failed);
        foreach (var dir in paths.Select(Path.GetDirectoryName).Distinct().OrderByDescending(d => d.Length)) DeleteIfEmpty(dir);
        AssetDatabase.Refresh();

        installed = FindInstalled();
        // Doi version thi giu symbol vi se cai lai ngay
        if (confirm && (removeAll || installed.Count == 0)) RemoveSymbol("USE_FIREBASE");
        status = $"Da xoa {paths.Length - failed.Count} file: {string.Join(", ", modules)}";
        if (failed.Count > 0) status += $"\nKhong xoa duoc {failed.Count} file, xem Console";
        foreach (var f in failed) Debug.LogWarning($"[FirebaseInstaller] Cannot delete {f}");
    }

    // "Assets/Firebase/Plugins/x.dll" -> "Assets/Firebase"
    static string TopFolder(string path)
    {
        var parts = path.Split('/');
        return parts.Length > 2 ? parts[0] + "/" + parts[1] : path;
    }

    public bool HasInstalled => installed.Count > 0;
    public void Detect() => installed = FindInstalled();

    static IEnumerable<string> ReadManifest(string path) =>
        File.ReadAllLines(path).Select(l => l.Trim().Replace('\\', '/')).Where(l => l.StartsWith("Assets/"));

    internal static void DeleteIfEmpty(string dir)
    {
        while (!string.IsNullOrEmpty(dir) && dir.Replace('\\', '/') != "Assets" && Directory.Exists(dir)
               && !Directory.EnumerateFileSystemEntries(dir).Any(e => !e.EndsWith(".meta")))
        {
            AssetDatabase.DeleteAsset(dir.Replace('\\', '/'));
            dir = Path.GetDirectoryName(dir);
        }
    }

    internal static void RemoveSymbol(string symbol)
    {
        var groups = new[] { EditorUserBuildSettings.selectedBuildTargetGroup, BuildTargetGroup.Standalone, BuildTargetGroup.Android, BuildTargetGroup.iOS };
        foreach (var group in groups.Distinct())
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Where(s => s.Length > 0).ToList();
            if (defines.Remove(symbol)) PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
        }
    }

    internal static void AddSymbol(string symbol)
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Where(s => s.Length > 0).ToList();
        if (defines.Contains(symbol)) return;
        defines.Add(symbol);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
    }

    // Firebase de lai file Firebase<Module>_version-x.y.z_manifest.txt sau khi import
    Dictionary<string, string> FindInstalled()
    {
        var found = AssetDatabase.FindAssets("_manifest")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => (Path: p, Match: Regex.Match(Path.GetFileName(p), @"^(Firebase\w+)_version-([\d\.]+)_manifest")))
            .Where(x => x.Match.Success)
            .GroupBy(x => x.Match.Groups[1].Value)
            .ToList();
        manifests = found.ToDictionary(g => g.Key, g => g.First().Path);
        return found.ToDictionary(g => g.Key, g => g.First().Match.Groups[2].Value);
    }

    void CancelRequest()
    {
        if (request == null) return;
        request.Abort();
        ClearRequest();
        EditorApplication.update -= Repaint;
        status = "Da huy";
    }

    void ClearRequest()
    {
        request?.Dispose();
        request = null;
        Repaint();
    }
}
#endif

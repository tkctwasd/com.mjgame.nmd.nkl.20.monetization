#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

// SDK phat hanh 1 file .unitypackage trong GitHub Releases
public abstract class SdkInstallerTab
{
    [Serializable] class Asset { public string name, browser_download_url; }
    [Serializable] class Release { public string tag_name; public bool prerelease, draft; public Asset[] assets; }
    [Serializable] class ReleaseList { public Release[] items; }

    // File dung chung voi SDK khac, khong xoa
    static readonly string[] SharedPrefixes = { "Assets/ExternalDependencyManager/", "Assets/PlayServicesResolver/" };

    readonly EditorWindow host;
    readonly string name, repo, symbol, rootFolder;
    readonly Func<string, string> tagToVersion;
    readonly Func<string, bool> assetFilter;
    readonly Func<string> detectVersion;
    readonly List<string> versions = new List<string>();
    readonly Dictionary<string, Asset> packages = new Dictionary<string, Asset>();
    string installed, status;
    int versionIndex;
    bool enableSymbol = true;
    UnityWebRequest request;

    protected SdkInstallerTab(EditorWindow host, string name, string repo, string symbol, Func<string, string> tagToVersion,
        Func<string, bool> assetFilter, Func<string> detectVersion, string rootFolder)
    {
        this.host = host;
        this.name = name;
        this.repo = repo;
        this.symbol = symbol;
        this.tagToVersion = tagToVersion;
        this.assetFilter = assetFilter;
        this.detectVersion = detectVersion;
        this.rootFolder = rootFolder;
    }

    string CacheDir => Path.Combine(FirebaseInstallerTab.CacheDir, name);
    string SessionKey => $"AdsInstaller.{name}.Releases";
    string CurrentVersion => versions.Count > 0 ? versions[versionIndex] : null;
    string PackagePath(string version) => version != null && packages.TryGetValue(version, out var a) ? Path.Combine(CacheDir, a.name) : null;
    bool IsCached(string version) => File.Exists(PackagePath(version) ?? "");
    void Repaint() => host.Repaint();

    public void Enable()
    {
        installed = detectVersion();
        var cached = SessionState.GetString(SessionKey, "");
        if (cached.Length == 0) FetchVersions();
        else ParseReleases(cached);
    }

    public void Disable() => CancelRequest();

    public SdkInstallerTab ExclusiveWith;
    public Action DrawExtra;
    public string[] KeepOnChange = Array.Empty<string>();
    string replacing;
    public string Name => name;
    public string InstalledVersion => installed;
    public bool HasInstalled => installed != null;
    SdkInstallerTab Blocker => ExclusiveWith != null && ExclusiveWith.HasInstalled ? ExclusiveWith : null;
    public void Detect() => installed = detectVersion();

    public void Draw()
    {
        var busy = request != null;

        EditorGUILayout.LabelField("Installed", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  {name}", installed ?? "Chua cai");

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, GUILayout.Width(60));
        GUI.enabled = !busy && versions.Count > 0;
        versionIndex = EditorGUILayout.Popup(versionIndex, versions.ToArray());
        GUI.enabled = !busy;
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) FetchVersions();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        if (CurrentVersion != null && packages.TryGetValue(CurrentVersion, out var asset))
            EditorGUILayout.LabelField("  File", asset.name, EditorStyles.miniLabel);

        enableSymbol = EditorGUILayout.ToggleLeft($"Bat {symbol} sau khi import", enableSymbol);

        EditorGUILayout.Space();
        if (DrawLatest(name, installed, versions.Count > 0 ? versions[0] : null, CurrentVersion)) versionIndex = 0;
        if (installed != null && CurrentVersion != null && installed != CurrentVersion)
        {
            var up = CompareVersion(CurrentVersion, installed) > 0;
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            EditorGUILayout.HelpBox($"Dang dung {name} {installed}. {(up ? "Update len" : "Ha xuong")} {CurrentVersion}?", MessageType.Warning);
            GUI.backgroundColor = Color.white;
            GUI.enabled = !busy;
            if (GUILayout.Button($"{(up ? "Update len" : "Ha xuong")} {CurrentVersion}", GUILayout.Height(28))) ChangeVersion();
            GUI.enabled = true;
        }
        var blocker = Blocker;
        if (blocker != null && installed == null)
        {
            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            EditorGUILayout.HelpBox($"Project dang dung {blocker.Name} {blocker.InstalledVersion}. Khong the cai them {name}.\nChi dung 1 trong 2: Adjust hoac AppsFlyer. Remove {blocker.Name} truoc.", MessageType.Error);
            GUI.backgroundColor = Color.white;
        }
        GUI.enabled = !busy && CurrentVersion != null && installed == null && blocker == null;
        if (GUILayout.Button(IsCached(CurrentVersion) ? "Import" : "Download & Import", GUILayout.Height(30))) DownloadAndImport();
        GUI.enabled = !busy && installed != null;
        if (GUILayout.Button($"Remove {name}", GUILayout.Height(24))) Remove();
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
        if (installed != null) DrawExtra?.Invoke();
    }

    void FetchVersions()
    {
        status = "Dang lay danh sach version...";
        request = UnityWebRequest.Get($"https://api.github.com/repos/{repo}/releases?per_page=30");
        request.SetRequestHeader("User-Agent", "Unity");
        request.SendWebRequest().completed += _ =>
        {
            if (request == null) return;
            if (request.result != UnityWebRequest.Result.Success) status = $"Loi lay version: {request.error}";
            else
            {
                SessionState.SetString(SessionKey, request.downloadHandler.text);
                ParseReleases(request.downloadHandler.text);
            }
            ClearRequest();
        };
    }

    void ParseReleases(string json)
    {
        versions.Clear();
        packages.Clear();
        var list = JsonUtility.FromJson<ReleaseList>("{\"items\":" + json + "}");
        foreach (var r in list.items ?? Array.Empty<Release>())
        {
            if (r.prerelease || r.draft) continue;
            var a = r.assets?.FirstOrDefault(x => assetFilter(x.name));
            if (a == null) continue;
            var v = tagToVersion(r.tag_name);
            versions.Add(v);
            packages[v] = a;
        }
        versionIndex = Mathf.Max(0, installed != null ? versions.IndexOf(installed) : 0);
        status = versions.Count > 0 ? null : "Khong lay duoc version";
    }

    // Go ban dang cai roi cai version dang chon
    void ChangeVersion()
    {
        var version = CurrentVersion;
        var up = CompareVersion(version, installed) > 0;
        var source = IsCached(version) ? $"Dung file co san: {PackagePath(version)}" : $"Tai {packages[version].name} ve {CacheDir}";
        if (!EditorUtility.DisplayDialog($"{(up ? "Update" : "Ha version")} {name}",
                $"{name}: {installed} -> {version}\n\nImport ban {version} de len ban {installed}, chi xoa file cu khong con dung.{KeepNote}\n{source}", up ? "Update" : "Ha version", "Huy")) return;

        replacing = installed;
        installed = null;
        DownloadAndImport(false);
    }

    // Xanh: dang dung ban moi nhat, cam: chua moi nhat (tra ve true khi bam chon ban moi)
    public static bool DrawLatest(string label, string installed, string latest, string selected)
    {
        if (latest == null) return false;
        if (installed == null)
        {
            EditorGUILayout.HelpBox($"Moi nhat: {label} {latest}", MessageType.Info);
            return false;
        }
        var outdated = CompareVersion(latest, installed) > 0;
        GUI.backgroundColor = outdated ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 1f, 0.4f);
        EditorGUILayout.HelpBox(outdated
            ? $"Chua moi nhat: dang dung {label} {installed}, moi nhat {latest}"
            : $"Dang dung ban moi nhat: {label} {installed}", outdated ? MessageType.Warning : MessageType.Info);
        GUI.backgroundColor = Color.white;
        return outdated && selected != latest && GUILayout.Button($"Chon ban moi {latest}");
    }

    public static int CompareVersion(string a, string b)
    {
        Version.TryParse(Regex.Replace(a ?? "", "[^0-9.]", ""), out var va);
        Version.TryParse(Regex.Replace(b ?? "", "[^0-9.]", ""), out var vb);
        return (va ?? new Version()).CompareTo(vb ?? new Version());
    }

    void DownloadAndImport(bool confirm = true)
    {
        var version = CurrentVersion;
        if (installed != null || Blocker != null) return;
        var path = PackagePath(version);
        var source = IsCached(version) ? $"Dung file co san: {path}" : $"Tai {packages[version].name} ve {CacheDir}";
        var symbolNote = enableSymbol ? $"\nBat {symbol}" : "";
        if (confirm && !EditorUtility.DisplayDialog($"Import {name}", $"Cai {name} {version}\n\n{source}{symbolNote}", "Cai", "Huy")) return;
        if (IsCached(version))
        {
            Import(path);
            return;
        }

        Directory.CreateDirectory(CacheDir);
        var temp = path + ".part";
        request = new UnityWebRequest(packages[version].browser_download_url, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerFile(temp) { removeFileOnAbort = true }
        };
        status = $"Dang tai {packages[version].name}...";
        EditorApplication.update += Repaint;
        request.SendWebRequest().completed += _ =>
        {
            EditorApplication.update -= Repaint;
            if (request == null) return;
            var ok = request.result == UnityWebRequest.Result.Success;
            var error = request.error;
            ClearRequest();
            if (!ok)
            {
                status = $"Tai that bai: {error}";
                RestoreReplacing();
                return;
            }
            File.Move(temp, path);
            Import(path);
        };
    }

    string KeepNote => KeepOnChange.Length > 0 ? $"\nGiu nguyen: {string.Join(", ", KeepOnChange)}" : "";

    void Import(string path)
    {
        if (replacing != null) DeleteObsolete(replacing, path);
        replacing = null;
        // Danh dau de code sau khi compile lai biet vua import (vd MAX tu migrate sang UPM)
        SessionState.SetBool($"AdsInstaller.{name}.JustImported", true);
        AdsInstallerWindow.SetPending($"cai {name} {CurrentVersion}");
        AssetDatabase.ImportPackage(path, false);
        if (enableSymbol) FirebaseInstallerTab.AddSymbol(symbol);
        status = $"Import {Path.GetFileName(path)}";
        installed = CurrentVersion;
    }

    // Xoa dung cac file trong .unitypackage da cache; khong co cache thi xoa thu muc goc cua SDK
    void Remove(bool confirm = true)
    {

        var package = PackagePath(installed);
        var files = File.Exists(package ?? "") ? ReadPackagePaths(package) : new List<string>();
        var paths = files
            .Where(p => !SharedPrefixes.Any(p.StartsWith) && !p.StartsWith(rootFolder + "/") && File.Exists(p))
            .ToList();
        if (Directory.Exists(rootFolder)) paths.Add(rootFolder);

        var shown = paths.Take(15).ToList();
        var more = paths.Count > shown.Count ? $"\n... va {paths.Count - shown.Count} muc khac" : "";
        var note = File.Exists(package ?? "") ? "" : "\n\n(Khong co file cache, chi xoa thu muc goc)";
        if (confirm && !EditorUtility.DisplayDialog($"Remove {name}",
                $"Xoa {name} {installed}:\n{string.Join("\n", shown)}{more}\n\nGo {symbol}{note}", "Xoa", "Huy")) return;

        if (confirm) AdsInstallerWindow.SetPending($"xoa {name}");
        var failed = new List<string>();
        AssetDatabase.DeleteAssets(paths.Distinct().ToArray(), failed);
        foreach (var dir in paths.Select(Path.GetDirectoryName).Distinct().OrderByDescending(d => d.Length))
            FirebaseInstallerTab.DeleteIfEmpty(dir);
        AssetDatabase.Refresh();

        // Doi version thi giu symbol vi se cai lai ngay
        if (confirm) FirebaseInstallerTab.RemoveSymbol(symbol);
        installed = detectVersion();
        status = $"Da xoa {name} ({paths.Count - failed.Count} muc)";
        if (failed.Count > 0) status += $"\nKhong xoa duoc {failed.Count} muc, xem Console";
        foreach (var f in failed) Debug.LogWarning($"[AdsInstaller] Cannot delete {f}");
    }

    // File co trong ban cu ma ban moi khong co; can .unitypackage ban cu trong cache, khong co thi chi import de len
    void DeleteObsolete(string oldVersion, string newPackage)
    {
        var oldPackage = PackagePath(oldVersion);
        if (!File.Exists(oldPackage ?? "")) return;
        var keep = new HashSet<string>(ReadPackagePaths(newPackage));
        var obsolete = ReadPackagePaths(oldPackage)
            .Where(p => !keep.Contains(p) && File.Exists(p)
                        && !SharedPrefixes.Any(p.StartsWith) && !KeepOnChange.Any(p.StartsWith))
            .ToArray();
        if (obsolete.Length == 0) return;
        var failed = new List<string>();
        AssetDatabase.DeleteAssets(obsolete, failed);
        foreach (var dir in obsolete.Select(Path.GetDirectoryName).Distinct().OrderByDescending(d => d.Length))
            FirebaseInstallerTab.DeleteIfEmpty(dir);
        Debug.Log($"[AdsInstaller] {name}: xoa {obsolete.Length - failed.Count} file cu cua ban {oldVersion}");
    }

    // .unitypackage la tar.gz, moi asset co file <guid>/pathname chua duong dan trong project
    static List<string> ReadPackagePaths(string package)
    {
        var result = new List<string>();
        var header = new byte[512];
        using (var gz = new GZipStream(File.OpenRead(package), CompressionMode.Decompress))
        {
            while (ReadFull(gz, header) == 512)
            {
                var entry = Encoding.ASCII.GetString(header, 0, 100).TrimEnd('\0');
                if (entry.Length == 0) break;
                var size = Convert.ToInt64(Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' ').PadLeft(1, '0'), 8);
                var data = new byte[(size + 511) / 512 * 512];
                ReadFull(gz, data);
                if (entry.EndsWith("/pathname"))
                    result.Add(Encoding.UTF8.GetString(data, 0, (int)size).Split('\n')[0].Trim().Replace('\\', '/'));
            }
        }
        return result;
    }

    static int ReadFull(Stream s, byte[] buffer)
    {
        var total = 0;
        int n;
        while (total < buffer.Length && (n = s.Read(buffer, total, buffer.Length - total)) > 0) total += n;
        return total;
    }

    public static string FindVersionInFile(string path, string pattern)
    {
        if (!File.Exists(path)) return null;
        var m = Regex.Match(File.ReadAllText(path), pattern);
        return m.Success ? m.Groups[1].Value : null;
    }

    public static string FindVersionInScript(string scriptName, string pattern)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{scriptName} t:MonoScript"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != scriptName) continue;
            var m = Regex.Match(File.ReadAllText(path), pattern);
            if (m.Success) return m.Groups[1].Value;
        }
        return null;
    }

    void CancelRequest()
    {
        if (request == null) return;
        request.Abort();
        ClearRequest();
        EditorApplication.update -= Repaint;
        status = "Da huy";
        RestoreReplacing();
    }

    // Tai loi/huy khi dang doi version: ban cu van nguyen trong project
    void RestoreReplacing()
    {
        if (replacing == null) return;
        installed = replacing;
        replacing = null;
    }

    void ClearRequest()
    {
        request?.Dispose();
        request = null;
        Repaint();
    }
}
#endif

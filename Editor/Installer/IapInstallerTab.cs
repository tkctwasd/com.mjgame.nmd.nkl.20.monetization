#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// Unity IAP cai qua Package Manager (Unity Registry), khong tai file
public class IapInstallerTab
{
    const string PackageId = "com.unity.purchasing", Symbol = "USE_IAP";

    readonly EditorWindow host;
    string[] versions = Array.Empty<string>();
    string installed, status;
    int versionIndex;
    bool enableSymbol = true;
    Request request;
    Action<Request> onDone;

    public IapInstallerTab(EditorWindow host) => this.host = host;

    public bool HasInstalled => installed != null;
    string CurrentVersion => versions.Length > 0 ? versions[versionIndex] : null;

    public void Detect()
    {
        var info = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == PackageId);
        installed = info?.version;
    }

    public void Enable()
    {
        Detect();
        if (versions.Length == 0) FetchVersions();
    }

    public void Disable()
    {
        EditorApplication.update -= Poll;
        request = null;
    }

    public void Draw()
    {
        var busy = request != null;

        EditorGUILayout.LabelField("Installed", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  In-App Purchasing", installed ?? "Chua cai");

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Version", EditorStyles.boldLabel, GUILayout.Width(60));
        GUI.enabled = !busy && versions.Length > 0;
        versionIndex = EditorGUILayout.Popup(versionIndex, versions);
        GUI.enabled = !busy;
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) FetchVersions();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("  Package", PackageId, EditorStyles.miniLabel);

        enableSymbol = EditorGUILayout.ToggleLeft($"Bat {Symbol} sau khi install", enableSymbol);

        EditorGUILayout.Space();
        if (SdkInstallerTab.DrawLatest("IAP", installed, versions.Length > 0 ? versions[0] : null, CurrentVersion)) versionIndex = 0;
        if (installed != null && CurrentVersion != null && installed != CurrentVersion)
        {
            var up = SdkInstallerTab.CompareVersion(CurrentVersion, installed) > 0;
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            EditorGUILayout.HelpBox($"Dang dung IAP {installed}. {(up ? "Update len" : "Ha xuong")} {CurrentVersion}?", MessageType.Warning);
            GUI.backgroundColor = Color.white;
            GUI.enabled = !busy;
            if (GUILayout.Button($"{(up ? "Update len" : "Ha xuong")} {CurrentVersion}", GUILayout.Height(28))) ChangeVersion(up);
            GUI.enabled = true;
        }
        GUI.enabled = !busy && CurrentVersion != null && installed == null;
        if (GUILayout.Button("Install", GUILayout.Height(30))) Install();
        GUI.enabled = !busy && installed != null;
        if (GUILayout.Button("Remove IAP", GUILayout.Height(24))) Remove();
        GUI.enabled = true;

        if (busy) EditorGUILayout.HelpBox("Package Manager dang xu ly...", MessageType.None);
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
    }

    void FetchVersions()
    {
        status = "Dang lay danh sach version...";
        Run(Client.Search(PackageId), r =>
        {
            var info = ((SearchRequest)r).Result?.FirstOrDefault();
            if (info == null)
            {
                status = $"Loi lay version: {r.Error?.message}";
                return;
            }
            // Chi lay ban release, moi nhat len dau
            versions = info.versions.compatible.Where(v => !v.Contains("-")).Reverse().ToArray();
            versionIndex = Mathf.Max(0, installed != null ? Array.IndexOf(versions, installed) : 0);
            status = versions.Length > 0 ? null : "Khong co version";
        });
    }

    void Install()
    {
        var version = CurrentVersion;
        var symbolNote = enableSymbol ? $"\nBat {Symbol}" : "";
        if (!EditorUtility.DisplayDialog("Install IAP", $"Cai {PackageId}@{version} tu Unity Registry{symbolNote}", "Cai", "Huy")) return;

        status = $"Dang cai {version}...";
        Run(Client.Add($"{PackageId}@{version}"), r =>
        {
            if (r.Status == StatusCode.Failure)
            {
                status = $"Cai that bai: {r.Error?.message}";
                return;
            }
            installed = version;
            AdsInstallerWindow.SetPending($"cai IAP {version}");
            if (enableSymbol) FirebaseInstallerTab.AddSymbol(Symbol);
            status = $"Da cai IAP {version}";
        });
    }

    // Package Manager doi thang version, khong can go truoc
    void ChangeVersion(bool up)
    {
        var version = CurrentVersion;
        if (!EditorUtility.DisplayDialog($"{(up ? "Update" : "Ha version")} IAP",
                $"{PackageId}: {installed} -> {version}", up ? "Update" : "Ha version", "Huy")) return;

        status = $"Dang doi sang {version}...";
        Run(Client.Add($"{PackageId}@{version}"), r =>
        {
            if (r.Status == StatusCode.Failure)
            {
                status = $"Doi version that bai: {r.Error?.message}";
                return;
            }
            installed = version;
            AdsInstallerWindow.SetPending($"doi IAP sang {version}");
            status = $"Da doi IAP sang {version}";
        });
    }

    void Remove()
    {
        if (!EditorUtility.DisplayDialog("Remove IAP", $"Xoa {PackageId}@{installed} khoi project\nGo {Symbol}", "Xoa", "Huy")) return;

        // Go symbol truoc de code USE_IAP khong loi compile khi package bi xoa
        FirebaseInstallerTab.RemoveSymbol(Symbol);
        status = "Dang xoa...";
        Run(Client.Remove(PackageId), r =>
        {
            if (r.Status == StatusCode.Failure)
            {
                status = $"Xoa that bai: {r.Error?.message}";
                return;
            }
            installed = null;
            AdsInstallerWindow.SetPending("xoa IAP");
            status = "Da xoa IAP";
        });
    }

    void Run(Request r, Action<Request> done)
    {
        request = r;
        onDone = done;
        EditorApplication.update += Poll;
    }

    void Poll()
    {
        if (request == null || !request.IsCompleted) return;
        EditorApplication.update -= Poll;
        var r = request;
        request = null;
        onDone?.Invoke(r);
        host.Repaint();
    }
}
#endif

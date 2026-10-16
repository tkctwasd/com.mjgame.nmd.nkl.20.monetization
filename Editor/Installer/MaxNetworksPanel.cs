#if UNITY_EDITOR && USE_APPLOVIN
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AppLovinMax.Scripts.IntegrationManager.Editor;
using UnityEditor;
using UnityEngine;
using Network = AppLovinMax.Scripts.IntegrationManager.Editor.Network;

public partial class AdsInstallerWindow
{
    partial void SetupAppLovinExtras() => sdks[0].DrawExtra = new MaxNetworksPanel(this).Draw;
}

// Sau khi Ads Installer import MAX (.unitypackage) thi tu chay "Upgrade All Adapters and Migrate to UPM"
[InitializeOnLoad]
static class MaxAutoMigrate
{
    const string Key = "AdsInstaller.AppLovin.JustImported";
    static readonly MethodInfo Migrate = typeof(AppLovinPluginMigrationHelper).GetMethod("MigrateToUnityPackageManager",
        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

    static bool running;
    static double nextTry;

    static MaxAutoMigrate() => EditorApplication.update += Poll;

    // IsPluginInPackageManager tim asset theo label moi lan goi (cham), cache lai; Refresh sau moi lan load/migrate
    static bool? inUpm;
    public static bool InUpm => inUpm ?? (inUpm = AppLovinIntegrationManager.IsPluginInPackageManager).Value;
    public static void RefreshUpm() => inUpm = null;
    public static bool NeedsMigration => !InUpm;

    // Cho Unity import/compile xong roi moi migrate; giu co cho den khi migrate thanh cong
    static void Poll()
    {
        if (running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextTry) return;
        if (!SessionState.GetBool(Key, false))
        {
            EditorApplication.update -= Poll;
            return;
        }
        if (!NeedsMigration)
        {
            SessionState.EraseBool(Key);
            return;
        }
        MigrateNow();
    }

    public static void MigrateNow()
    {
        if (running) return;
        if (Migrate == null)
        {
            Debug.LogError("[AdsInstaller] Khong tim thay AppLovinPluginMigrationHelper.MigrateToUnityPackageManager");
            SessionState.EraseBool(Key);
            return;
        }
        running = true;
        AdsInstallerWindow.SetPending("migrate MAX sang UPM");
        AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(data =>
        {
            running = false;
            if (data == null)
            {
                Debug.LogWarning("[AdsInstaller] MAX: khong tai duoc plugin data, thu lai sau 10s");
                nextTry = EditorApplication.timeSinceStartup + 10;
                return;
            }
            SessionState.EraseBool(Key);
            // UPM cua MAX da kem External Dependency Manager, xoa ban trong Assets de khong bi trung
            var deleteEdm = System.IO.Directory.Exists("Assets/ExternalDependencyManager");
            Debug.Log("[AdsInstaller] MAX: tu dong Upgrade All Adapters and Migrate to UPM");
            Migrate.Invoke(null, new object[] { data, deleteEdm });
        }));
    }
}

// Danh sach Mediated Networks giong AppLovin Integration Manager
public class MaxNetworksPanel
{
    const BindingFlags Internal = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static readonly MethodInfo AddNetwork = typeof(AppLovinPackageManager).GetMethod("AddNetwork", Internal);
    static readonly MethodInfo RemoveNetwork = typeof(AppLovinPackageManager).GetMethod("RemoveNetwork", Internal);

    readonly EditorWindow host;
    PluginData data;
    bool loading, expanded = true, busy;

    // Network dang duoc tro chuot vao nut Version, de khong mo menu lap lai
    string hovered;
    GUIStyle stateStyle;

    public MaxNetworksPanel(EditorWindow host)
    {
        this.host = host;
    }

    static bool IsInstalled(Network n) => n.CurrentVersions != null && !string.IsNullOrEmpty(n.CurrentVersions.Unity);
    static bool NeedsUpgrade(Network n) => IsInstalled(n) && n.CurrentToLatestVersionComparisonResult == MaxSdkUtils.VersionComparisonResult.Lesser;

    // Network mac dinh hien san trong bang (theo DisplayName); network da cai luon nam trong bang
    const string PickedKey = "AdsInstaller.MaxPicked";
    static readonly string[] DefaultPicked = { "BIGO Ads", "Google Ad Manager", "Google AdMob", "InMobi", "ironSource", "Liftoff Monetize", "Mintegral", "Pangle", "Unity Ads" };
    // Luu SessionState de khong bi reset khi Unity compile lai
    readonly HashSet<string> picked = new HashSet<string>(
        SessionState.GetString(PickedKey, null)?.Split(';').Where(x => x.Length > 0) ?? DefaultPicked);

    void SavePicked() => SessionState.SetString(PickedKey, string.Join(";", picked));

    public void Draw()
    {
        if (stateStyle == null) stateStyle = new GUIStyle(EditorStyles.miniLabel);
        if (data == null && !loading) Load();

        EditorGUILayout.Space();
        var networks = data?.MediatedNetworks ?? new Network[0];
        var rows = networks.Where(n => IsInstalled(n) || picked.Contains(n.DisplayName)).OrderBy(n => n.DisplayName).ToList();

        EditorGUILayout.BeginHorizontal();
        expanded = EditorGUILayout.Foldout(expanded, $"Mediated Networks ({networks.Count(IsInstalled)} installed)", true, EditorStyles.foldoutHeader);
        GUI.enabled = !busy && !loading;
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) Load();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!expanded) return;
        if (MaxAutoMigrate.NeedsMigration)
        {
            EditorGUILayout.HelpBox("MAX dang nam trong Assets, chua chuyen sang Unity Package Manager.", MessageType.Warning);
            GUI.enabled = !busy;
            if (GUILayout.Button("Upgrade All Adapters and Migrate to UPM", GUILayout.Height(24))
                && EditorUtility.DisplayDialog("Migrate to UPM", "Chuyen MAX va adapter sang Unity Package Manager?", "Migrate", "Huy"))
                MaxAutoMigrate.MigrateNow();
            GUI.enabled = true;
        }
        if (loading)
        {
            EditorGUILayout.HelpBox("Dang tai danh sach network...", MessageType.None);
            return;
        }
        if (data == null)
        {
            EditorGUILayout.HelpBox("Khong tai duoc danh sach network. Bam Refresh.", MessageType.Warning);
            return;
        }

        // Xo xuong chon network chua co trong bang
        var options = networks.Where(n => !rows.Contains(n)).OrderBy(n => n.DisplayName).ToList();
        GUI.enabled = !busy && options.Count > 0;
        var labels = new[] { "+ Them network..." }.Concat(options.Select(n => n.DisplayName)).ToArray();
        var pick = EditorGUILayout.Popup(0, labels);
        if (pick > 0) { picked.Add(options[pick - 1].DisplayName); SavePicked(); }
        GUI.enabled = true;

        if (rows.Count == 0)
        {
            EditorGUILayout.HelpBox("Chua co network nao. Chon o tren de them.", MessageType.None);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        foreach (var n in rows)
        {
            var installed = IsInstalled(n);
            var upgrade = NeedsUpgrade(n);
            var state = n.IsCurrentlyInstalling ? "Dang cai..." : !installed ? "Chua cai" : upgrade ? $"Can update -> {n.LatestVersions?.Unity}" : "Moi nhat";
            var color = !installed ? Color.white : upgrade ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 1f, 0.4f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(n.DisplayName, EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField(installed ? n.CurrentVersions.Unity : "-", EditorStyles.miniLabel, GUILayout.Width(150));
            stateStyle.normal.textColor = color;
            var style = stateStyle;
            EditorGUILayout.LabelField(state, style);
            // Chua cai: Install; da cai: Upgrade (chi sang khi co ban moi)
            var verb = installed ? "Upgrade" : "Install";
            GUI.enabled = !busy && (!installed || upgrade) && !n.IsCurrentlyInstalling;
            if (GUILayout.Button(verb, GUILayout.Width(64)) &&
                EditorUtility.DisplayDialog($"{verb} {n.DisplayName}",
                    installed ? $"{n.CurrentVersions.Unity} -> {n.LatestVersions?.Unity}" : $"Cai {n.DisplayName} {n.LatestVersions?.Unity}", verb, "Huy"))
                AppLovinEditorCoroutine.StartCoroutine(Run(n));
            // Chon version cu hon/khac: chi khi MAX o UPM (moi adapter la package android + ios)
            GUI.enabled = !busy && installed && MaxAutoMigrate.InUpm && n.Packages != null && n.Packages.Length > 0;
            var vRect = GUILayoutUtility.GetRect(new GUIContent("Version ▾"), GUI.skin.button, GUILayout.Width(72));
            if (GUI.Button(vRect, versionsLoading.Contains(n.Name) ? "..." : "Version ▾")) ShowVersionMenu(n, vRect);
            GUI.enabled = !busy;
            // Chua cai: X chi bo khoi bang; da cai: X go adapter
            if (GUILayout.Button("X", GUILayout.Width(22)))
            {
                if (installed) Remove(n);
                else { picked.Remove(n.DisplayName); SavePicked(); }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        var toInstall = rows.Where(n => !IsInstalled(n)).ToArray();
        var toUpgrade = rows.Where(NeedsUpgrade).ToArray();
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !busy && toInstall.Length > 0;
        if (GUILayout.Button($"Install ({toInstall.Length})", GUILayout.Height(26))) InstallAll(toInstall);
        GUI.enabled = !busy && toUpgrade.Length > 0;
        if (GUILayout.Button(toUpgrade.Length > 0 ? $"Upgrade All ({toUpgrade.Length})" : "Da moi nhat", GUILayout.Height(26))) InstallAll(toUpgrade);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    void Load()
    {
        loading = true;
        AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(d =>
        {
            data = d;
            loading = false;
            MaxAutoMigrate.RefreshUpm();
            host.Repaint();
            // Tai san danh sach version cho network da cai de bam Version ▾ mo ngay
            foreach (var n in d?.MediatedNetworks?.Where(IsInstalled) ?? Enumerable.Empty<Network>()) Prefetch(n, null);
        }));
    }

    void InstallAll(Network[] list)
    {
        var lines = list.Select(n => IsInstalled(n)
            ? $"Update {n.DisplayName}: {n.CurrentVersions.Unity} -> {n.LatestVersions?.Unity}"
            : $"Install {n.DisplayName} {n.LatestVersions?.Unity}");
        if (!EditorUtility.DisplayDialog("Install / Update adapter", string.Join("\n", lines), "OK", "Huy")) return;
        AppLovinEditorCoroutine.StartCoroutine(Run(list));
    }

    IEnumerator Run(params Network[] list)
    {
        busy = true;
        foreach (var n in list)
        {
            n.IsCurrentlyInstalling = true;
            host.Repaint();
            yield return (IEnumerator)AddNetwork.Invoke(null, new object[] { n, false });
            n.IsCurrentlyInstalling = false;
        }
        busy = false;
        Load();
    }

    readonly Dictionary<string, string[]> packageVersions = new Dictionary<string, string[]>();
    readonly HashSet<string> versionsLoading = new HashSet<string>();
    const string Registry = "https://unity.packages.applovin.com/";
    string lastError;

    // Version UPM cua adapter ma hoa so: 6000100.0.0 -> 6.0.1.0, 803000401.0.0 -> 8.3.0.4.1
    static string Decode(string upm)
    {
        var digits = upm.Split('.')[0];
        var parts = new List<string>();
        for (var end = digits.Length; end > 0; end -= 2)
        {
            var start = Mathf.Max(0, end - 2);
            parts.Insert(0, int.Parse(digits.Substring(start, end - start)).ToString());
        }
        return string.Join(".", parts);
    }

    static string Platform(string package) => package.EndsWith(".ios") ? "iOS" : "Android";

    // Danh sach version luu SessionState: mo lai cua so / compile lai khong phai tai lai
    bool HasVersions(string package)
    {
        if (packageVersions.TryGetValue(package, out var known) && known.Length > 0) return true;
        var cached = SessionState.GetString("AdsInstaller.MaxVersions." + package, null);
        if (string.IsNullOrEmpty(cached)) return false;
        packageVersions[package] = cached.Split(';').Where(x => x.Length > 0).ToArray();
        return true;
    }

    // Tai truoc danh sach version cua network (chay ngam sau khi load / install)
    void Prefetch(Network n, System.Action done)
    {
        if (n.Packages == null || n.Packages.Length == 0 || !MaxAutoMigrate.InUpm) return;
        var missing = n.Packages.Select(p => p.Name).Where(x => !HasVersions(x)).ToArray();
        if (missing.Length == 0)
        {
            done?.Invoke();
            return;
        }
        if (!versionsLoading.Add(n.Name)) return;
        var pending = missing.Length;
        foreach (var name in missing)
        {
            // Client.Search khong tra ve version cua scoped registry nen hoi thang registry AppLovin
            var req = UnityEngine.Networking.UnityWebRequest.Get(Registry + name);
            req.SetRequestHeader("Accept", "application/json");
            req.SendWebRequest();
            void Poll()
            {
                if (!req.isDone) return;
                EditorApplication.update -= Poll;
                var versions = req.result == UnityEngine.Networking.UnityWebRequest.Result.Success
                    ? System.Text.RegularExpressions.Regex.Matches(req.downloadHandler.text, "\"version\"\\s*:\\s*\"(\\d+\\.\\d+\\.\\d+)\"")
                        .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).Distinct()
                        .OrderByDescending(v => long.Parse(v.Split('.')[0])).ToArray()
                    : new string[0];
                lastError = versions.Length == 0 ? $"{name}: {req.error ?? "khong co version"}" : null;
                if (lastError != null) Debug.LogWarning("[AdsInstaller] Khong lay duoc version " + lastError);
                req.Dispose();
                // Khong luu ket qua rong de lan sau thu lai
                if (versions.Length > 0)
                {
                    packageVersions[name] = versions;
                    SessionState.SetString("AdsInstaller.MaxVersions." + name, string.Join(";", versions));
                }
                if (--pending > 0) return;
                versionsLoading.Remove(n.Name);
                host.Repaint();
                done?.Invoke();
            }
            EditorApplication.update += Poll;
        }
    }

    void ShowVersionMenu(Network n, Rect at)
    {
        var names = n.Packages.Select(p => p.Name).ToArray();
        // Chi tai 1 lan, tai loi thi hien menu voi phan da co
        // Chua co danh sach thi tai, xong la tro chuot lai se mo (khong mo menu ngoai OnGUI)
        if (!names.All(HasVersions) && !versionsLoading.Contains(n.Name))
        {
            Prefetch(n, () => { if (hovered == n.Name) hovered = null; });
            if (!names.Any(HasVersions)) return;
        }

        var installed = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().ToDictionary(p => p.name, p => p.version);
        var columns = new List<VersionPopup.Column>();
        foreach (var name in names)
        {
            installed.TryGetValue(name, out var current);
            if (!packageVersions.TryGetValue(name, out var list)) continue;
            var package = name;
            columns.Add(new VersionPopup.Column
            {
                Title = Platform(name),
                Items = list.Select(v => (Decode(v), v == current, (System.Action)(() => SetPackageVersion(n, package, current, v)))).ToList()
            });
        }
        // Mo sang ben phai nut de khong che cac dong ben duoi
        var right = new Rect(at.xMax + 2, at.y - 2, 0, 0);
        PopupWindow.Show(right, new VersionPopup(columns, lastError, () => { if (hovered == n.Name) hovered = null; }));
    }

    void SetPackageVersion(Network n, string package, string current, string version)
    {
        if (current == version) return;
        var label = $"{n.DisplayName} {Platform(package)}: {(current != null ? Decode(current) : "-")} -> {Decode(version)}";
        if (!EditorUtility.DisplayDialog("Doi version adapter", label, "Doi", "Huy")) return;

        busy = true;
        AdsInstallerWindow.SetPending($"doi {label}");
        var req = UnityEditor.PackageManager.Client.Add($"{package}@{version}");
        void Poll()
        {
            if (!req.IsCompleted) return;
            EditorApplication.update -= Poll;
            busy = false;
            if (req.Status == UnityEditor.PackageManager.StatusCode.Failure) Debug.LogError($"[AdsInstaller] {label}: {req.Error?.message}");
            Load();
        }
        EditorApplication.update += Poll;
    }

    void Remove(Network n)
    {
        if (!EditorUtility.DisplayDialog($"Remove {n.DisplayName}", $"Xoa adapter {n.DisplayName} {n.CurrentVersions.Unity} khoi project?", "Xoa", "Huy")) return;
        RemoveNetwork.Invoke(null, new object[] { n });
        // Go xong thi bo khoi bang luon, ke ca network mac dinh
        picked.Remove(n.DisplayName);
        SavePicked();
        AdsInstallerWindow.SetPending($"xoa adapter {n.DisplayName}");
        Load();
    }
}
// Popup chon version: Android | iOS 2 cot, dong khi bam ra ngoai
public class VersionPopup : PopupWindowContent
{
    public class Column
    {
        public string Title;
        public List<(string Label, bool Current, System.Action Select)> Items;
    }

    const float ColumnWidth = 150f, Row = 20f, MaxRows = 15;
    readonly List<Column> columns;
    readonly string error;
    readonly System.Action onClose;
    Vector2 scroll;

    public VersionPopup(List<Column> columns, string error, System.Action onClose)
    {
        this.columns = columns;
        this.error = error;
        this.onClose = onClose;
    }

    public override void OnClose() => onClose?.Invoke();

    public override Vector2 GetWindowSize()
    {
        if (columns.Count == 0) return new Vector2(320, 44);
        var rows = Mathf.Min(MaxRows, columns.Max(c => c.Items.Count));
        return new Vector2(ColumnWidth * columns.Count + 24, rows * Row + 32);
    }

    public override void OnOpen()
    {
        editorWindow.wantsMouseMove = true;
    }

    public override void OnGUI(Rect rect)
    {
        if (columns.Count == 0)
        {
            EditorGUILayout.HelpBox("Khong lay duoc danh sach version" + (error != null ? "\n" + error : ""), MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        foreach (var c in columns) EditorGUILayout.LabelField(c.Title, EditorStyles.boldLabel, GUILayout.Width(ColumnWidth));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
        EditorGUILayout.BeginHorizontal();
        foreach (var c in columns)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ColumnWidth));
            foreach (var item in c.Items)
            {
                var style = item.Current ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(item.Current ? "✓ " + item.Label : item.Label, style, GUILayout.Height(Row - 2)))
                {
                    editorWindow.Close();
                    item.Select();
                    GUIUtility.ExitGUI();
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        if (Event.current.type == EventType.MouseMove) editorWindow.Repaint();
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click setup cho module _Monetization. File này TỰ ĐỨNG ĐỘC LẬP: kéo riêng 1 mình
/// vào bất kỳ project Unity nào cũng chạy được (không tham chiếu class nào của module).
/// Mở qua menu: Tools > Monetization > Setup.
///
/// Việc mà tool tự làm:
///   1. Dò các SDK đang có trong project (MaxSdk, Firebase, Newtonsoft, Unity IAP, Adjust, AppsFlyer).
///   2. CHỈ bật scripting define khi SDK tương ứng đã có mặt -> tránh vỡ compile.
///   3. Thêm scoped registry (AppLovin, OpenUPM) + UPM dependencies vào Packages/manifest.json
///      -> Unity tự tải Newtonsoft / Unity IAP / AppLovin MAX.
///   4. Tải & import tự động Adjust / AppsFlyer từ GitHub release mới nhất.
///
/// Còn lại làm tay: Firebase (là zip + cần EULA + google-services.json / GoogleService-Info.plist).
/// </summary>
public class MonetizationSetup : EditorWindow
{
    // ---- Định nghĩa dependency -------------------------------------------------

    private class Dep
    {
        public string Title;        // tên hiển thị
        public string TypeProbe;    // full type name để dò xem SDK đã có chưa
        public string Define;       // scripting define bật khi có SDK (null nếu không cần)
        public bool Required;       // bắt buộc để compile được module
        public string Url;          // link cài đặt / tài liệu
        public string Github;       // "owner/repo" nếu SDK có .unitypackage trên GitHub release -> tải & import tự động
        public string Note;
    }

    private static readonly Dep[] Deps =
    {
        new Dep {
            Title = "AppLovin MAX SDK", TypeProbe = "MaxSdkBase", Define = null, Required = true,
            Url = "https://dash.applovin.com/documentation/mediation/unity/getting-started/integration",
            Note = "Bắt buộc cho Ads. Cài qua Package Manager sau khi đã thêm scoped registry bên dưới."
        },
        new Dep {
            Title = "Firebase Analytics", TypeProbe = "Firebase.Analytics.FirebaseAnalytics",
            Define = null, Required = true,
            Url = "https://firebase.google.com/download/unity",
            Note = "Bắt buộc. Import FirebaseAnalytics.unitypackage, thêm google-services.json / GoogleService-Info.plist."
        },
        new Dep {
            Title = "Firebase RemoteConfig", TypeProbe = "Firebase.RemoteConfig.FirebaseRemoteConfig",
            Define = null, Required = true,
            Url = "https://firebase.google.com/download/unity",
            Note = "Bắt buộc. Import FirebaseRemoteConfig.unitypackage."
        },
        new Dep {
            Title = "Firebase Crashlytics", TypeProbe = "Firebase.Crashlytics.Crashlytics",
            Define = null, Required = true,
            Url = "https://firebase.google.com/download/unity",
            Note = "Bắt buộc. Import FirebaseCrashlytics.unitypackage."
        },
        new Dep {
            Title = "Newtonsoft Json", TypeProbe = "Newtonsoft.Json.JsonConvert",
            Define = null, Required = true,
            Url = "com.unity.nuget.newtonsoft-json",
            Note = "Bắt buộc. Là UPM package, tool tự thêm được."
        },
        new Dep {
            Title = "Unity IAP (Purchasing)", TypeProbe = "UnityEngine.Purchasing.StandardPurchasingModule",
            Define = "USE_IAP", Required = false,
            Url = "com.unity.purchasing",
            Note = "Tùy chọn. Cần cho hệ thống IAP. UPM package, tool tự thêm được."
        },
        new Dep {
            Title = "Adjust SDK", TypeProbe = "AdjustSdk.Adjust", Define = "USE_ADJUST", Required = false,
            Url = "https://github.com/adjust/unity_sdk/releases", Github = "adjust/unity_sdk",
            Note = "Tùy chọn. Cần cho attribution + ad/iap revenue tracking qua Adjust. Có thể Tải & import tự động."
        },
        new Dep {
            Title = "AppsFlyer SDK", TypeProbe = "AppsFlyerSDK.AppsFlyer", Define = "USE_APPSFLYER", Required = false,
            Url = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin/releases", Github = "AppsFlyerSDK/appsflyer-unity-plugin",
            Note = "Tùy chọn. Cần cho attribution + ad/iap revenue tracking qua AppsFlyer. Có thể Tải & import tự động."
        },
    };

    // Build target groups mà ta set define lên.
    private static readonly BuildTargetGroup[] TargetGroups =
    {
        BuildTargetGroup.Android,
        BuildTargetGroup.iOS,
        BuildTargetGroup.Standalone,
    };

    // UPM packages có thể thêm tự động (name -> version).
    private static readonly (string name, string version)[] UpmPackages =
    {
        ("com.unity.nuget.newtonsoft-json", "3.2.1"),
        ("com.unity.purchasing", "5.2.1"),
        ("com.applovin.mediation.ads", "8.6.4"),
    };

    // Scoped registries cần cho AppLovin + OpenUPM (external-dependency-manager).
    private static readonly (string name, string url, string[] scopes)[] ScopedRegistries =
    {
        ("AppLovin MAX Unity", "https://unity.packages.applovin.com/",
            new[] { "com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp" }),
        ("package.openupm.com", "https://package.openupm.com",
            new[] { "com.google.external-dependency-manager" }),
    };

    // ---- Menu / Window ---------------------------------------------------------

    [MenuItem("Tools/Monetization/Setup", priority = 0)]
    public static void Open()
    {
        var w = GetWindow<MonetizationSetup>(true, "Monetization Setup");
        w.minSize = new Vector2(560, 620);
        w.Show();
    }

    private Vector2 _scroll;

    // Cache danh sách version theo repo (đã fetch từ GitHub) + index đang chọn.
    private readonly Dictionary<string, string[]> _versions = new Dictionary<string, string[]>();
    private readonly Dictionary<string, int> _versionIdx = new Dictionary<string, int>();

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Monetization — Setup nhanh", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chạy theo thứ tự:\n" +
            "1) 'Thêm scoped registry + UPM deps' rồi để Unity resolve xong.\n" +
            "2) Import các SDK còn thiếu (Firebase / Adjust / AppsFlyer) từ link.\n" +
            "3) 'Auto set scripting defines' — chỉ bật define cho SDK đã có mặt.",
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("① Thêm scoped registry + UPM deps", GUILayout.Height(34)))
                PatchManifest();
            if (GUILayout.Button("③ Auto set scripting defines", GUILayout.Height(34)))
                AutoSetDefines(log: true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Trạng thái dependency", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var d in Deps)
            DrawDepRow(d);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scripting defines hiện tại (build group đang chọn):", EditorStyles.miniBoldLabel);
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        EditorGUILayout.SelectableLabel(
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group),
            EditorStyles.textField, GUILayout.Height(18));
    }

    private void DrawDepRow(Dep d)
    {
        bool present = TypeExists(d.TypeProbe);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = present ? "✔" : "✘";
                var color = present ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.4f, 0.35f);
                var prev = GUI.color; GUI.color = color;
                EditorGUILayout.LabelField(icon, GUILayout.Width(18));
                GUI.color = prev;

                var tag = d.Required ? "[BẮT BUỘC]" : "[tùy chọn]";
                EditorGUILayout.LabelField($"{d.Title}  {tag}", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(d.Define))
                {
                    bool defineOn = HasDefine(d.Define);
                    EditorGUILayout.LabelField(defineOn ? $"{d.Define} ✓" : d.Define,
                        EditorStyles.miniLabel, GUILayout.Width(120));
                }

                bool isUpm = d.Url != null && d.Url.StartsWith("com.");
                if (!present)
                {
                    if (isUpm)
                    {
                        if (GUILayout.Button("Add (UPM)", GUILayout.Width(110)))
                            UnityEditor.PackageManager.Client.Add(d.Url);
                    }
                    else if (!string.IsNullOrEmpty(d.Github))
                    {
                        var opts = _versions.TryGetValue(d.Github, out var v) ? v : new[] { "latest" };
                        if (!_versionIdx.ContainsKey(d.Github)) _versionIdx[d.Github] = 0;

                        _versionIdx[d.Github] = EditorGUILayout.Popup(_versionIdx[d.Github], opts, GUILayout.Width(110));
                        if (GUILayout.Button(new GUIContent("⟳", "Lấy danh sách version từ GitHub"), GUILayout.Width(26)))
                            FetchVersions(d.Github);

                        if (GUILayout.Button("Tải & import", GUILayout.Width(100)))
                        {
                            var sel = opts[Mathf.Clamp(_versionIdx[d.Github], 0, opts.Length - 1)];
                            DownloadAndImportFromGithub(d.Title, d.Github, sel == "latest" ? null : sel);
                        }
                    }
                    else if (GUILayout.Button("Mở link", GUILayout.Width(110)))
                        Application.OpenURL(d.Url);
                }
            }
            EditorGUILayout.LabelField(d.Note, EditorStyles.wordWrappedMiniLabel);
        }
    }

    // ---- Scripting defines -----------------------------------------------------

    /// <summary>Bật define cho SDK đã có mặt, tắt define cho SDK bị thiếu (an toàn compile).</summary>
    public static void AutoSetDefines(bool log)
    {
        var sb = new StringBuilder();
        foreach (var group in TargetGroups)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            foreach (var d in Deps)
            {
                if (string.IsNullOrEmpty(d.Define)) continue;
                bool present = TypeExists(d.TypeProbe);
                if (present && !defines.Contains(d.Define)) defines.Add(d.Define);
                else if (!present && defines.Contains(d.Define)) defines.Remove(d.Define);
            }

            var joined = string.Join(";", defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joined);
            sb.AppendLine($"  {group}: {joined}");
        }

        if (log)
            Debug.Log("[MonetizationSetup] Đã set scripting defines:\n" + sb);
    }

    private static bool HasDefine(string symbol)
    {
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
            .Split(';').Contains(symbol);
    }

    // ---- SDK detection ---------------------------------------------------------

    private static bool TypeExists(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return false;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.GetType(fullName, false) != null) return true;
            }
            catch { /* một số dynamic assembly ném lỗi khi GetType, bỏ qua */ }
        }
        return false;
    }

    // ---- Tải & import .unitypackage từ GitHub release --------------------------

    private static WebClient NewClient()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        var wc = new WebClient();
        wc.Headers.Add("User-Agent", "MonetizationSetup");   // GitHub API bắt buộc User-Agent
        return wc;
    }

    /// <summary>Lấy danh sách tag từ GitHub release của repo, nạp vào dropdown (mục đầu luôn là "latest").</summary>
    private void FetchVersions(string repo)
    {
        try
        {
            EditorUtility.DisplayProgressBar(repo, "Đang lấy danh sách version từ GitHub...", 0.5f);
            string json;
            using (var wc = NewClient())
                json = wc.DownloadString($"https://api.github.com/repos/{repo}/releases?per_page=40");
            EditorUtility.ClearProgressBar();

            var tags = Regex.Matches(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"")
                .Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            if (tags.Count == 0) throw new Exception("Repo không có release nào.");

            tags.Insert(0, "latest");
            _versions[repo] = tags.ToArray();
            _versionIdx[repo] = 0;
            Debug.Log($"[MonetizationSetup] {repo}: lấy được {tags.Count - 1} version.");
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[MonetizationSetup] Lấy version {repo} lỗi: {e.Message}");
        }
    }

    /// <summary>
    /// Tải .unitypackage từ GitHub release rồi import tự động.
    /// tag = null -> release mới nhất; ngược lại tải đúng tag đó.
    /// Dùng cho SDK phân phối dạng .unitypackage (Adjust, AppsFlyer) — không có trên UPM.
    /// </summary>
    private static void DownloadAndImportFromGithub(string title, string repo, string tag)
    {
        var tmp = "";
        try
        {
            var api = string.IsNullOrEmpty(tag)
                ? $"https://api.github.com/repos/{repo}/releases/latest"
                : $"https://api.github.com/repos/{repo}/releases/tags/{tag}";

            using (var wc = NewClient())
            {
                EditorUtility.DisplayProgressBar(title, $"Đang hỏi GitHub ({tag ?? "latest"})...", 0.2f);
                var json = wc.DownloadString(api);

                // Tìm URL asset .unitypackage đầu tiên trong release.
                var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.unitypackage)\"");
                if (!m.Success)
                    throw new Exception("Release này không có file .unitypackage. Tải tay từ trang release.");

                var url = m.Groups[1].Value;
                var realTag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
                tmp = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));

                EditorUtility.DisplayProgressBar(title, $"Đang tải {realTag} ...", 0.6f);
                wc.DownloadFile(url, tmp);
            }

            EditorUtility.ClearProgressBar();
            // interactive:false -> import thẳng, không hỏi từng file.
            AssetDatabase.ImportPackage(tmp, false);
            Debug.Log($"[MonetizationSetup] Đã tải & import {title} ({tag ?? "latest"}) từ {repo}.");
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[MonetizationSetup] Tải {title} lỗi: {e.Message}");
            if (EditorUtility.DisplayDialog(title,
                    $"Tải tự động thất bại:\n{e.Message}\n\nMở trang release để tải tay?", "Mở trang", "Đóng"))
                Application.OpenURL($"https://github.com/{repo}/releases");
        }
    }

    // ---- manifest.json patching (không phụ thuộc Newtonsoft) -------------------

    private static string ManifestPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

    private static void PatchManifest()
    {
        var path = ManifestPath;
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Monetization Setup", "Không tìm thấy Packages/manifest.json.", "OK");
            return;
        }

        string text = File.ReadAllText(path);
        string original = text;

        // 1) UPM dependencies: thêm dòng nếu package name chưa xuất hiện.
        var toAddDeps = UpmPackages.Where(p => !text.Contains($"\"{p.name}\"")).ToList();
        if (toAddDeps.Count > 0)
        {
            int depsIdx = text.IndexOf("\"dependencies\"", StringComparison.Ordinal);
            int brace = depsIdx >= 0 ? text.IndexOf('{', depsIdx) : -1;
            if (brace >= 0)
            {
                var insert = new StringBuilder();
                foreach (var p in toAddDeps)
                    insert.Append($"\n    \"{p.name}\": \"{p.version}\",");
                text = text.Insert(brace + 1, insert.ToString());
            }
        }

        // 2) Scoped registries: thêm registry nếu url chưa có.
        var toAddReg = ScopedRegistries.Where(r => !text.Contains(r.url)).ToList();
        if (toAddReg.Count > 0)
            text = InsertScopedRegistries(text, toAddReg);

        if (text == original)
        {
            EditorUtility.DisplayDialog("Monetization Setup",
                "manifest.json đã đầy đủ scoped registry + UPM deps. Không cần thay đổi.", "OK");
            return;
        }

        File.WriteAllText(path + ".bak", original);          // backup an toàn
        File.WriteAllText(path, text);
        AssetDatabase.Refresh();
        UnityEditor.PackageManager.Client.Resolve();
        Debug.Log($"[MonetizationSetup] Đã cập nhật manifest.json (backup: manifest.json.bak). Unity đang resolve packages...");
        EditorUtility.DisplayDialog("Monetization Setup",
            "Đã cập nhật Packages/manifest.json.\nĐợi Unity resolve/import xong, sau đó chạy 'Auto set scripting defines'.", "OK");
    }

    private static string InsertScopedRegistries(string text, List<(string name, string url, string[] scopes)> regs)
    {
        var blocks = regs.Select(BuildRegistryJson).ToList();

        int srIdx = text.IndexOf("\"scopedRegistries\"", StringComparison.Ordinal);
        if (srIdx >= 0)
        {
            // đã có mảng scopedRegistries -> chèn phần tử vào ngay sau '['
            int bracket = text.IndexOf('[', srIdx);
            if (bracket >= 0)
            {
                // mảng rỗng "[]" -> không thêm dấu phẩy cuối; có phần tử -> thêm phẩy để ngăn cách.
                int nxt = bracket + 1;
                while (nxt < text.Length && char.IsWhiteSpace(text[nxt])) nxt++;
                bool empty = nxt < text.Length && text[nxt] == ']';

                var insert = "\n" + string.Join(",\n", blocks) + (empty ? "\n" : ",");
                return text.Insert(bracket + 1, insert);
            }
        }

        // chưa có -> thêm mảng scopedRegistries ở cấp gốc, ngay sau block dependencies.
        int depsIdx = text.IndexOf("\"dependencies\"", StringComparison.Ordinal);
        int open = text.IndexOf('{', depsIdx);
        int close = FindMatchingBrace(text, open);
        if (close < 0) return text; // không parse được, bỏ qua an toàn

        // Dấu phẩy ĐỨNG TRƯỚC để ngăn cách với block "dependencies" phía trên;
        // KHÔNG có dấu phẩy cuối (tránh trailing comma nếu dependencies là key cuối).
        var arr = ",\n  \"scopedRegistries\": [\n" + string.Join(",\n", blocks) + "\n  ]";
        return text.Insert(close + 1, arr);
    }

    private static string BuildRegistryJson((string name, string url, string[] scopes) r)
    {
        var scopes = string.Join(",\n", r.scopes.Select(s => $"        \"{s}\""));
        return
            "    {\n" +
            $"      \"name\": \"{r.name}\",\n" +
            $"      \"url\": \"{r.url}\",\n" +
            "      \"scopes\": [\n" +
            scopes + "\n" +
            "      ]\n" +
            "    }";
    }

    private static int FindMatchingBrace(string text, int openIdx)
    {
        int depth = 0;
        for (int i = openIdx; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }
}
#endif

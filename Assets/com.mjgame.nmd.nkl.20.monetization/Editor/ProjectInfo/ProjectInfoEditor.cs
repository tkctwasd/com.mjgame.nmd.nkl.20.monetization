#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AppLovinMax.Scripts.IntegrationManager.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public class ProjectInfoEditor : EditorWindow
{
    private enum Status { Ok, Missing, Update }

    private class Row
    {
        public string[] Cells;
        public Status Status;
        public string Note;
    }

    private class Section
    {
        public string Title;
        public string[] Header;
        public List<Row> Rows = new List<Row>();
        public bool Foldout = true;
        public Rect ContentRect;

        public void Add(params string[] cells)
        {
            var missing = cells.Skip(1).Any(IsMissing);
            Rows.Add(new Row
            {
                Cells = cells.Select((c, i) => i > 0 && IsMissing(c) ? "-" : c).ToArray(),
                Status = missing ? Status.Missing : Status.Ok
            });
        }

        public void AddUpdate(string note, params string[] cells)
        {
            Add(cells);
            var row = Rows[Rows.Count - 1];
            if (row.Status == Status.Ok) { row.Status = Status.Update; row.Note = note; }
        }

        public void Space() => Rows.Add(new Row { Cells = new string[0] });
    }

    // Chup bang cach tu cuon scroll view tung doan roi ghep anh, nen card dai hon window van chup du
    private class CaptureJob
    {
        public string Label;
        public Section Section;
        public bool Crop;
        public bool Started;
        public RectInt Area;
        public Texture2D Texture;
        public int NextY;
        public bool WaitingRepaint;
        public Vector2 OldScroll;
    }

    private class Adapter
    {
        public string Network;
        public string Android;
        public string Ios;
    }

    private static readonly string[] KnownSymbols = { "USE_IAP", "USE_ADJUST", "USE_APPSFLYER", "FIREBASE_ANALYTIC" };
    private static readonly string[] Tabs = { "Marketing", "Details" };

    // Gia tri mac dinh cua AdConstants/AdjustManager/AppsFlyerManager = chua dien
    private static readonly HashSet<string> Placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "-", "Not found", "sdk-key", "admob-android-id", "admob-ios-id", "Your Game Name", "com.yourcompany.yourgame",
        "https://yourcompany.com/privacy", "https://yourcompany.com/terms", "app-open-id", "banner-id", "mrec-id",
        "interstitial-id", "rewarded-id", "id_adjust", "dev-key-appsflyer", "id-app"
    };

    private static PluginData pluginData;
    private static bool pluginDataLoading;

    private readonly List<Section> sections = new List<Section>();
    private readonly List<Section> marketingSections = new List<Section>();
    private Vector2 scroll;
    private int tab;
    private CaptureJob capture;
    private Rect viewportScreenRect;
    private GUIStyle titleStyle, headerStyle, keyStyle, valueStyle;

    private static bool IsMissing(string value) => string.IsNullOrWhiteSpace(value) || Placeholders.Contains(value.Trim());

    [MenuItem("Tools/Monetization/Project Info")]
    public static void Open()
    {
        var window = GetWindow<ProjectInfoEditor>("Project Info");
        window.minSize = new Vector2(620, 500);
        window.Collect();
    }

    private void OnEnable()
    {
        Collect();
        if (pluginData == null) LoadPluginData();
    }

    private void OnFocus() => Collect();

    private void LoadPluginData()
    {
        if (pluginDataLoading) return;
        pluginDataLoading = true;
        AppLovinEditorCoroutine.StartCoroutine(AppLovinIntegrationManager.Instance.LoadPluginData(data =>
        {
            pluginDataLoading = false;
            if (data != null) pluginData = data;
            Collect();
        }));
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        tab = GUILayout.Toolbar(tab, Tabs, EditorStyles.toolbarButton, GUILayout.Width(160));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("AdConstants", EditorStyles.toolbarButton)) SelectAdConstants();
        if (GUILayout.Button("MAX Manager", EditorStyles.toolbarButton)) OpenMaxManager();
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
        {
            LoadPluginData();
            Collect();
        }
        if (GUILayout.Button("Copy All", EditorStyles.toolbarButton))
        {
            EditorGUIUtility.systemCopyBuffer = BuildReport(tab == 0 ? marketingSections : sections);
            ShowNotification(new GUIContent("Copied"));
        }
        GUI.enabled = tab == 0 && capture == null;
        if (GUILayout.Button("Screenshot All", EditorStyles.toolbarButton)) StartCapture(null, false);
        if (GUILayout.Button("Crop All", EditorStyles.toolbarButton)) StartCapture(null, true);
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tab == 0) DrawMarketing();
        else DrawDetails();
        EditorGUILayout.EndScrollView();

        if (Event.current.type == EventType.Repaint)
        {
            var view = GUILayoutUtility.GetLastRect();
            viewportScreenRect = new Rect(GUIUtility.GUIToScreenPoint(view.position), view.size);

            // Chup sau khi frame da ve xong de anh khong bi lech
            if (capture != null && capture.WaitingRepaint)
            {
                capture.WaitingRepaint = false;
                EditorApplication.delayCall += CaptureStep;
            }
        }
    }

    private static void OpenMaxManager() => EditorApplication.ExecuteMenuItem("AppLovin/Integration Manager");

    #region Marketing

    private static readonly Color Orange = new Color(1f, 0.6f, 0.1f);

    private void DrawMarketing()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, normal = { textColor = Orange } };
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = default };
            keyStyle = new GUIStyle(EditorStyles.label) { fontSize = default };
            valueStyle = new GUIStyle(EditorStyles.label) { fontSize = default };
        }

        // Card rong vua noi dung de anh chup khong thua khoang trong, paste vao Excel chu khong bi nho
        const float columnGap = 24f, updateWidth = 150f, buttonWidth = 148f;
        var keyWidth = marketingSections.SelectMany(s => s.Rows).Where(r => r.Cells.Length > 0)
            .Select(r => keyStyle.CalcSize(new GUIContent(r.Cells[0])).x)
            .DefaultIfEmpty(0).Max() + columnGap;
        var columnWidths = marketingSections.ToDictionary(s => s, s => GetColumnWidths(s, columnGap));
        var cardWidth = marketingSections.Max(s =>
            Mathf.Max(titleStyle.CalcSize(new GUIContent(s.Title)).x + buttonWidth,
                keyWidth + columnWidths[s].Sum() + (s.Rows.Any(r => r.Status == Status.Update) ? updateWidth : 0f))) + 12f;

        foreach (var section in marketingSections)
        {
            var widths = columnWidths[section];
            EditorGUILayout.Space(6);
            // Binh thuong card full width; chi khi chup Crop moi thu gon vua noi dung
            if (capture != null && capture.Crop) EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(cardWidth));
            else EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(section.Title, titleStyle, GUILayout.Height(26));
            if (capture != null) GUILayout.Space(buttonWidth);
            else
            {
                if (GUILayout.Button("Screenshot", GUILayout.Width(80))) StartCapture(section, false);
                if (GUILayout.Button("Crop", GUILayout.Width(60))) StartCapture(section, true);
            }
            EditorGUILayout.EndHorizontal();

            if (section.Header != null) DrawCells(section.Header, headerStyle, headerStyle, Orange, null, keyWidth, widths);
            foreach (var row in section.Rows)
            {
                if (row.Cells.Length == 0) { EditorGUILayout.Space(6); continue; }
                DrawRow(row, keyWidth, widths);
            }

            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                section.ContentRect = GUILayoutUtility.GetLastRect();
            }
        }
    }

    // Do rong tung cot gia tri (khong tinh cot key) theo text dai nhat
    private float[] GetColumnWidths(Section section, float gap)
    {
        var rows = section.Rows.Where(r => r.Cells.Length > 1).Select(r => r.Cells).ToList();
        if (section.Header != null) rows.Add(section.Header);
        var count = rows.Count == 0 ? 0 : rows.Max(r => r.Length) - 1;
        var widths = new float[count];
        foreach (var cells in rows)
            for (var i = 1; i < cells.Length; i++)
            {
                var style = cells == section.Header ? headerStyle : valueStyle;
                widths[i - 1] = Mathf.Max(widths[i - 1], style.CalcSize(new GUIContent(cells[i])).x + gap);
            }
        return widths;
    }

    private void DrawRow(Row row, float keyWidth, float[] widths)
    {
        var color = row.Status == Status.Missing ? new Color(1f, 0.35f, 0.35f)
            : row.Status == Status.Update ? new Color(1f, 0.85f, 0.2f)
            : Color.white;
        DrawCells(row.Cells, keyStyle, valueStyle, color, row, keyWidth, widths);
    }

    private static void DrawCells(string[] cells, GUIStyle first, GUIStyle others, Color color, Row row, float keyWidth, float[] widths)
    {
        var keyColor = new GUIStyle(first) { normal = { textColor = color } };
        var valueColor = new GUIStyle(others) { normal = { textColor = row != null && row.Status == Status.Missing ? Color.white : color } };

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(cells[0], keyColor, GUILayout.Width(keyWidth));
        for (var i = 1; i < cells.Length; i++)
            EditorGUILayout.LabelField(cells[i], valueColor, GUILayout.Width(widths[i - 1]), GUILayout.Height(18));
        if (row != null && row.Status == Status.Update && GUILayout.Button(row.Note, GUILayout.Width(146))) OpenMaxManager();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private static Section CollectMarketingGame()
    {
        var s = new Section { Title = "Game Info" };
        var target = EditorUserBuildSettings.activeBuildTarget;
        var isIos = target == BuildTarget.iOS;
        s.Add("Game Name", PlayerSettings.productName);
        // Android chua tung set rieng thi Unity tra ve mac dinh com.Company.Product, nen chi lay theo nen tang dang build
        s.Add(isIos ? "Bundle ID (iOS)" : "Package Name (Android)",
            PlayerSettings.GetApplicationIdentifier(isIos ? BuildTargetGroup.iOS : BuildTargetGroup.Android));
        s.Add("Version", PlayerSettings.bundleVersion);
        s.Add(isIos ? "Build (iOS)" : "Build (Android)", isIos ? PlayerSettings.iOS.buildNumber : PlayerSettings.Android.bundleVersionCode.ToString());
        return s;
    }

    private static Section CollectMarketingAdIds()
    {
        var ad = FindAsset<AdConstants>();
        var s = new Section { Title = ad != null ? $"Ad IDs ({ad.name})" : "Ad IDs (AdConstants not found)" };
        s.Add("Interstitial", ad != null ? ad.InterstitialId : null);
        s.Add("Rewarded", ad != null ? ad.RewardedId : null);
        s.Add("MREC", ad != null ? ad.MRECId : null);
        s.Add("App Open (AOA)", ad != null ? ad.AppOpenId : null);
        s.Add("Banner", ad != null ? ad.BannerId : null);

        // Chi hien MMP dang bat; neu chua bat cai nao thi hien ca hai de biet dang thieu
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';');
        var useAdjust = defines.Contains("USE_ADJUST");
        var useAppsFlyer = defines.Contains("USE_APPSFLYER");
        if (!useAdjust && !useAppsFlyer) useAdjust = useAppsFlyer = true;
        if (useAdjust) s.Add("Adjust Token", FindSerializedValue("AdjustManager", "adjust"));
        if (useAppsFlyer)
        {
            s.Add("AppsFlyer Dev Key", FindSerializedValue("AppsFlyerManager", "devKey"));
            s.Add("AppsFlyer App ID", FindSerializedValue("AppsFlyerManager", "appID"));
        }
        return s;
    }

    private static Section CollectMarketingMaxPlugin()
    {
        var s = new Section { Title = "AppLovin MAX Plugin Details", Header = new[] { "Platform", "Current Version" } };
        GetMaxNativeVersions(out var android, out var ios);
        var latest = pluginData?.AppLovinMax?.LatestVersions;
        AddVersionRow(s, "Unity 3D", MaxSdk.Version, latest?.Unity);
        AddVersionRow(s, "Android", android, latest?.Android);
        AddVersionRow(s, "iOS", ios, latest?.Ios);
        return s;
    }

    private static void AddVersionRow(Section s, string key, string current, string latest)
    {
        if (!IsMissing(current) && !string.IsNullOrEmpty(latest)
            && MaxSdkUtils.CompareVersions(current, latest) == MaxSdkUtils.VersionComparisonResult.Lesser)
            s.AddUpdate($"Update → {latest}", key, current);
        else s.Add(key, current);
    }

    private static Section CollectMarketingNetworks()
    {
        var title = "Mediated Networks (Installed)";
        if (pluginDataLoading) title += " - checking updates...";
        else if (pluginData == null) title += " - offline, no update check";
        var s = new Section { Title = title, Header = new[] { "Network", "Android", "iOS" } };

        if (pluginData != null)
        {
            var installed = pluginData.MediatedNetworks
                .Where(n => n.CurrentVersions != null && !string.IsNullOrEmpty(n.CurrentVersions.Unity))
                .OrderBy(n => n.DisplayName);
            foreach (var n in installed)
            {
                var android = string.IsNullOrEmpty(n.CurrentVersions.Android) ? "-" : n.CurrentVersions.Android;
                var ios = string.IsNullOrEmpty(n.CurrentVersions.Ios) ? "-" : n.CurrentVersions.Ios;
                // Network chi co 1 nen tang (vd CSJ chi iOS) se co "-" nhung khong phai thieu
                var row = new Row { Cells = new[] { n.DisplayName, android, ios } };
                if (n.CurrentToLatestVersionComparisonResult == MaxSdkUtils.VersionComparisonResult.Lesser)
                {
                    row.Status = Status.Update;
                    row.Note = $"Update → {n.LatestVersions.Unity}";
                }
                s.Rows.Add(row);
            }
        }
        else
        {
            foreach (var a in GetAdapters()) s.Rows.Add(new Row { Cells = new[] { a.Network, a.Android, a.Ios } });
        }

        if (s.Rows.Count == 0) s.Add("Networks", null, null);
        return s;
    }

    private static Section CollectMarketingMaxSettings()
    {
        var s = new Section { Title = "AppLovin SDK & Privacy Settings" };
        var settings = AppLovinSettings.Instance;
        var internalSettings = AppLovinInternalSettings.Instance;
        s.Add("AppLovin SDK Key", settings != null ? settings.SdkKey : null);
        s.Add("Google App ID (Android)", settings != null ? settings.AdMobAndroidAppId : null);
        s.Add("Google App ID (iOS)", settings != null ? settings.AdMobIosAppId : null);
        s.Add("MAX Ad Review", settings != null ? settings.QualityServiceEnabled.ToString() : null);
        s.Space();
        s.Add("Terms & Privacy Flow", internalSettings != null ? internalSettings.ConsentFlowEnabled.ToString() : null);
        s.Add("Privacy Policy URL", internalSettings != null ? internalSettings.ConsentFlowPrivacyPolicyUrl : null);
        s.Add("Terms of Service URL", internalSettings != null ? internalSettings.ConsentFlowTermsOfServiceUrl : null);
        return s;
    }

    #endregion

    private void DrawDetails()
    {
        foreach (var section in sections)
        {
            section.Foldout = EditorGUILayout.BeginFoldoutHeaderGroup(section.Foldout, section.Title);
            if (section.Foldout)
            {
                EditorGUI.indentLevel++;
                foreach (var row in section.Rows)
                {
                    if (row.Cells.Length < 2) continue;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(row.Cells[0], GUILayout.Width(180));
                    EditorGUILayout.SelectableLabel(row.Cells[1], GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(40)))
                        EditorGUIUtility.systemCopyBuffer = row.Cells[1];
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);
        }
    }

    private void Collect()
    {
        sections.Clear();
        sections.Add(CollectApp());
        sections.Add(CollectBuild());
        sections.Add(CollectAppLovin());
        sections.Add(CollectFirebase());
        sections.Add(CollectRemoteConfig());
        sections.Add(CollectSymbols());
        sections.Add(CollectPackages());

        marketingSections.Clear();
        marketingSections.Add(CollectMarketingGame());
        marketingSections.Add(CollectMarketingAdIds());
        marketingSections.Add(CollectMarketingMaxPlugin());
        marketingSections.Add(CollectMarketingNetworks());
        marketingSections.Add(CollectMarketingMaxSettings());
        Repaint();
    }

    private static Section CollectApp()
    {
        var s = new Section { Title = "Application" };
        s.Add("Product Name", PlayerSettings.productName);
        s.Add("Company Name", PlayerSettings.companyName);
        s.Add("Version", PlayerSettings.bundleVersion);
        s.Add("Package (Android)", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        s.Add("Bundle ID (iOS)", PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
        s.Add("Unity Version", Application.unityVersion);
        return s;
    }

    private static Section CollectBuild()
    {
        var s = new Section { Title = "Build" };
        s.Add("Active Target", EditorUserBuildSettings.activeBuildTarget.ToString());
        s.Add("Android Version Code", PlayerSettings.Android.bundleVersionCode.ToString());
        s.Add("Android Min SDK", PlayerSettings.Android.minSdkVersion.ToString());
        s.Add("Android Target SDK", PlayerSettings.Android.targetSdkVersion.ToString());
        s.Add("Android Architectures", PlayerSettings.Android.targetArchitectures.ToString());
        s.Add("Build App Bundle (AAB)", EditorUserBuildSettings.buildAppBundle.ToString());
        s.Add("Keystore", PlayerSettings.Android.useCustomKeystore ? Path.GetFileName(PlayerSettings.Android.keystoreName) : "Debug");
        s.Add("iOS Build Number", PlayerSettings.iOS.buildNumber);
        s.Add("iOS Min Version", PlayerSettings.iOS.targetOSVersionString);
        s.Add("Scripting Backend", PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup).ToString());
        s.Add("Scenes In Build", EditorBuildSettings.scenes.Count(x => x.enabled).ToString());
        return s;
    }

    private static Section CollectAppLovin()
    {
        var s = new Section { Title = "AppLovin MAX" };
        GetMaxNativeVersions(out var android, out var ios);
        s.Add("MAX Unity Plugin", MaxSdk.Version);
        s.Add("MAX Android SDK", android);
        s.Add("MAX iOS SDK", ios);

        var settings = AppLovinSettings.Instance;
        if (settings != null)
        {
            s.Add("SDK Key (Settings)", settings.SdkKey);
            s.Add("AdMob Android App ID", settings.AdMobAndroidAppId);
            s.Add("AdMob iOS App ID", settings.AdMobIosAppId);
            s.Add("Quality Service", settings.QualityServiceEnabled.ToString());
        }

        var adConstants = FindAsset<AdConstants>();
        if (adConstants != null)
        {
            s.Add("AdConstants Asset", AssetDatabase.GetAssetPath(adConstants));
            s.Add("App Open ID", adConstants.AppOpenId);
            s.Add("Banner ID", adConstants.BannerId);
            s.Add("MREC ID", adConstants.MRECId);
            s.Add("Interstitial ID", adConstants.InterstitialId);
            s.Add("Rewarded ID", adConstants.RewardedId);
        }
        else s.Add("AdConstants Asset", "Not found");

        foreach (var a in GetAdapters()) s.Add($"Adapter {a.Network}", $"Android {a.Android} | iOS {a.Ios}");
        return s;
    }

    private static Section CollectFirebase()
    {
        var s = new Section { Title = "Firebase" };
        // Firebase Unity SDK de lai file Firebase<Module>_version-x.y.z_manifest.txt
        var manifests = AssetDatabase.FindAssets("_manifest")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(p => Regex.Match(Path.GetFileName(p), @"^(Firebase\w+)_version-([\d\.]+)_manifest"))
            .Where(m => m.Success)
            .GroupBy(m => m.Groups[1].Value);
        foreach (var m in manifests)
            s.Add(m.Key, m.First().Groups[2].Value);
        if (s.Rows.Count == 0) s.Add("SDK", "Not installed");

        s.Add("google-services.json", FindFile("google-services.json", "project_id"));
        s.Add("GoogleService-Info.plist", FindFile("GoogleService-Info.plist", "PROJECT_ID"));
        return s;
    }

    private static Section CollectRemoteConfig()
    {
        var s = new Section { Title = "Remote Config (Local Defaults)" };
        foreach (var name in new[] { "RemoteConfig", "RemoteConfig_Android", "RemoteConfig_iOS" })
        {
            var json = Resources.Load<TextAsset>(name);
            if (json == null) { s.Add(name, "Not found"); continue; }
            var keys = Regex.Matches(json.text, "\"(\\w+)\"\\s*:").Cast<Match>().Select(x => x.Groups[1].Value);
            s.Add(name, string.Join(", ", keys.Take(20)));
        }
        return s;
    }

    private static Section CollectSymbols()
    {
        var s = new Section { Title = "Define Symbols" };
        var group = EditorUserBuildSettings.selectedBuildTargetGroup;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';');
        foreach (var symbol in KnownSymbols)
            s.Add(symbol, defines.Contains(symbol) ? "ON" : "OFF");
        s.Add($"All ({group})", string.Join(";", defines));
        return s;
    }

    private static Section CollectPackages()
    {
        var s = new Section { Title = "Packages" };
        var monet = PackageInfo.FindForAssembly(typeof(AdsManager).Assembly);
        s.Add("Monetization", monet != null ? monet.version : ReadLocalPackageVersion());

        var manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages/manifest.json");
        if (File.Exists(manifestPath))
        {
            var text = File.ReadAllText(manifestPath);
            foreach (Match m in Regex.Matches(text, "\"(com\\.(?:unity\\.purchasing|unity\\.services\\.core|google\\.[\\w\\.]+|mjgame[\\w\\.]*|adjust[\\w\\.]*|appsflyer[\\w\\.]*))\"\\s*:\\s*\"([^\"]+)\""))
                s.Add(m.Groups[1].Value, m.Groups[2].Value);
        }
        return s;
    }

    // Tat ca Dependencies.xml cua MAX: trong Assets (import .unitypackage) hoac trong UPM package
    private static IEnumerable<string> GetMaxDependencyFiles()
    {
        var fromAssets = AssetDatabase.FindAssets("Dependencies")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith("Dependencies.xml"));
        var fromPackages = PackageInfo.GetAllRegisteredPackages()
            .Where(p => p.name.StartsWith("com.applovin"))
            .SelectMany(p => Directory.GetFiles(p.resolvedPath, "Dependencies.xml", SearchOption.AllDirectories));
        return fromAssets.Concat(fromPackages).Distinct();
    }

    private static void GetMaxNativeVersions(out string android, out string ios)
    {
        android = ios = null;
        foreach (var path in GetMaxDependencyFiles())
        {
            var xml = File.ReadAllText(path);
            var a = Regex.Match(xml, @"com\.applovin:applovin-sdk:\[?([\d\.]+)");
            if (!a.Success) continue;
            android = a.Groups[1].Value;
            ios = Regex.Match(xml, "iosPod name=\"AppLovinSDK\" version=\"([^\"]+)\"").Groups[1].Value;
            return;
        }
    }

    private static List<Adapter> GetAdapters()
    {
        var map = new SortedDictionary<string, Adapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in GetMaxDependencyFiles())
        {
            var network = GetNetworkName(path);
            if (network == null) continue;
            var xml = File.ReadAllText(path);
            if (!map.TryGetValue(network, out var adapter)) map[network] = adapter = new Adapter { Network = network };
            var android = Regex.Match(xml, "spec=\"com\\.applovin\\.mediation:[\\w\\-]+-adapter:\\[?([\\d\\.]+)");
            var ios = Regex.Match(xml, "iosPod name=\"AppLovinMediation\\w+Adapter\" version=\"([^\"]+)\"");
            if (android.Success) adapter.Android = android.Groups[1].Value;
            if (ios.Success) adapter.Ios = ios.Groups[1].Value;
        }
        return map.Values.Select(a => new Adapter { Network = a.Network, Android = a.Android ?? "-", Ios = a.Ios ?? "-" }).ToList();
    }

    private static string GetNetworkName(string dependencyPath)
    {
        var parts = dependencyPath.Replace('\\', '/').Split('/');
        // .unitypackage: MaxSdk/Mediation/<Network>/Editor/Dependencies.xml
        var mediation = Array.IndexOf(parts, "Mediation");
        if (mediation >= 0 && mediation + 1 < parts.Length - 1) return parts[mediation + 1];

        // UPM: com.applovin.mediation.adapters.<network>.<platform>, lay ten tu displayName
        var package = parts.FirstOrDefault(p => p.StartsWith("com.applovin.mediation.adapters."));
        if (package == null) return null;
        var info = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => package.StartsWith(p.name));
        if (info == null) return null;
        var name = Regex.Replace(info.displayName, @"^AppLovin Mediation\s+|\s+Adapter.*$", "");
        return string.IsNullOrEmpty(name) ? info.name : name;
    }

    // AdjustManager/AppsFlyerManager co the bi tat boi define nen tim theo ten type
    private static string FindSerializedValue(string typeName, string field)
    {
        var found = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .FirstOrDefault(x => x != null && x.GetType().Name == typeName);
        if (found == null)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                found = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                    .FirstOrDefault(x => x != null && x.GetType().Name == typeName);
                if (found != null) break;
            }
        }
        if (found == null) return "Not found";
        var prop = new SerializedObject(found).FindProperty(field);
        return prop != null ? prop.stringValue : "Not found";
    }

    private static void SelectAdConstants()
    {
        var ad = FindAsset<AdConstants>();
        if (ad == null) return;
        Selection.activeObject = ad;
        EditorGUIUtility.PingObject(ad);
    }

    private void StartCapture(Section section, bool crop)
    {
        if (capture != null || marketingSections.Count == 0) return;
        var label = section == null ? Tabs[tab] : Regex.Replace(section.Title.Split('-')[0], @"[^\w]+", "_").Trim('_');
        capture = new CaptureJob
        {
            Label = crop ? label + "_Crop" : label,
            Section = section,
            Crop = crop,
            OldScroll = scroll,
            WaitingRepaint = true
        };
        // Doi 1 frame de layout (full/crop, an nut) ve xong roi moi do vung chup
        Repaint();
    }

    private bool BeginCaptureArea()
    {
        var job = capture;
        var rect = job.Section != null ? job.Section.ContentRect
            : Rect.MinMaxRect(marketingSections.Min(x => x.ContentRect.xMin), marketingSections.First().ContentRect.yMin,
                marketingSections.Max(x => x.ContentRect.xMax), marketingSections.Last().ContentRect.yMax);
        var area = new RectInt(Mathf.FloorToInt(rect.x), Mathf.FloorToInt(rect.y), Mathf.CeilToInt(rect.width), Mathf.CeilToInt(rect.height));
        if (area.width < 1 || area.height < 1) return false;

        job.Started = true;
        job.Area = area;
        job.Texture = new Texture2D(area.width, area.height, TextureFormat.RGB24, false);
        job.NextY = area.y;
        return true;
    }

    private void ScrollToNext()
    {
        scroll = new Vector2(0, capture.NextY);
        capture.WaitingRepaint = true;
        Repaint();
    }

    private void CaptureStep()
    {
        var job = capture;
        if (job == null) return;
        if (!job.Started)
        {
            if (BeginCaptureArea()) ScrollToNext();
            else FinishCapture(false);
            return;
        }

        // Scroll view tu clamp scroll.y khi cuon het, nen tinh lai doan dang hien thi theo scroll thuc te
        var viewHeight = Mathf.FloorToInt(viewportScreenRect.height);
        var top = Mathf.Max(job.NextY, Mathf.RoundToInt(scroll.y));
        var bottom = Mathf.Min(job.Area.yMax, Mathf.RoundToInt(scroll.y) + viewHeight);
        if (bottom <= top)
        {
            FinishCapture(false);
            return;
        }

        // ReadScreenPixel nhan toa do theo point cua editor, khong nhan them pixelsPerPoint
        var screenPos = new Vector2(
            viewportScreenRect.x + job.Area.x - scroll.x,
            viewportScreenRect.y + top - scroll.y);
        var height = bottom - top;
        var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(screenPos, job.Area.width, height);

        // Texture2D tinh hang tu duoi len
        job.Texture.SetPixels(0, job.Area.yMax - bottom, job.Area.width, height, pixels);
        job.NextY = bottom;

        if (job.NextY >= job.Area.yMax) FinishCapture(true);
        else ScrollToNext();
    }

    private void FinishCapture(bool success)
    {
        var job = capture;
        capture = null;
        scroll = job.OldScroll;
        Repaint();

        if (success)
        {
            job.Texture.Apply();
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Screenshots");
            Directory.CreateDirectory(dir);
            var build = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? PlayerSettings.iOS.buildNumber : PlayerSettings.Android.bundleVersionCode.ToString();
            var file = Path.Combine(dir, $"{PlayerSettings.productName}_{PlayerSettings.bundleVersion}_{build}_{job.Label}.png");
            File.WriteAllBytes(file, job.Texture.EncodeToPNG());
            CopyImageToClipboard(file);
            EditorUtility.RevealInFinder(file);
            ShowNotification(new GUIContent("Copied to clipboard"));
            Debug.Log($"Screenshot saved: {file}");
        }
        else ShowNotification(new GUIContent("Screenshot failed"));
        if (job.Texture != null) DestroyImmediate(job.Texture);
    }

    // Unity khong co API copy anh vao clipboard nen goi script cua he dieu hanh, chay nen de khong block editor
    private static void CopyImageToClipboard(string file)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };
#if UNITY_EDITOR_WIN
        startInfo.FileName = "powershell";
        startInfo.Arguments = "-NoProfile -STA -Command \"Add-Type -AssemblyName System.Windows.Forms,System.Drawing; " +
                              $"[System.Windows.Forms.Clipboard]::SetImage([System.Drawing.Image]::FromFile('{file.Replace("'", "''")}'))\"";
#elif UNITY_EDITOR_OSX
        startInfo.FileName = "osascript";
        startInfo.Arguments = $"-e \"set the clipboard to (read (POSIX file \\\"{file}\\\") as «class PNGf»)\"";
#else
        return;
#endif
        try { System.Diagnostics.Process.Start(startInfo); }
        catch (Exception e) { Debug.LogWarning($"Copy screenshot to clipboard failed: {e.Message}"); }
    }

    private static string ReadLocalPackageVersion()
    {
        var path = AssetDatabase.FindAssets("package").Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.EndsWith("com.mjgame.nmd.nkl.20.monetization/package.json"));
        if (path == null) return null;
        return Regex.Match(File.ReadAllText(path), "\"version\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
    }

    private static string FindFile(string fileName, string projectIdKey)
    {
        var path = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories).FirstOrDefault();
        if (path == null) return "Not found";
        var text = File.ReadAllText(path);
        var id = Regex.Match(text, projectIdKey + "\"?\\s*[:>]\\s*(?:<string>)?\"?([\\w\\-]+)").Groups[1].Value;
        return string.IsNullOrEmpty(id) ? "Found" : $"Found (project: {id})";
    }

    private static T FindAsset<T>() where T : Object
    {
        var guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static string BuildReport(List<Section> list)
    {
        var sb = new StringBuilder();
        foreach (var section in list)
        {
            sb.AppendLine($"[{section.Title}]");
            if (section.Header != null) sb.AppendLine(string.Join(" | ", section.Header));
            foreach (var row in section.Rows)
            {
                if (row.Cells.Length == 0) continue;
                var line = row.Cells.Length == 2 ? $"{row.Cells[0]}: {row.Cells[1]}" : string.Join(" | ", row.Cells);
                if (row.Status == Status.Missing) line += "  (MISSING)";
                if (row.Status == Status.Update) line += $"  ({row.Note})";
                sb.AppendLine(line);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
#endif

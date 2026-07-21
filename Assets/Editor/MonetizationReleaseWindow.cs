#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Nút export release: đóng gói _Monetization -> Assets/Release, ghi version + changelog.
/// Mở qua menu: Tools > Monetization > Release (Export + Changelog).
///
/// Luồng: bấm Export -> commit -> push. CI (.gitlab-ci.yml) đọc version.txt + CHANGELOG_LATEST.md
/// để tự tạo GitLab Release + tag khi version thay đổi.
/// </summary>
public class MonetizationReleaseWindow : EditorWindow
{
    private const string ModuleFolder = "Assets/_Monetization";

    private static string ReleaseFolderAbs => Path.Combine(Application.dataPath, "Release");
    private static string VersionFile => Path.Combine(ReleaseFolderAbs, "version.txt");
    private static string ChangelogLatest => Path.Combine(ReleaseFolderAbs, "CHANGELOG_LATEST.md");
    private static string ChangelogFull => Path.Combine(ReleaseFolderAbs, "CHANGELOG.md");

    private string _version = "1.0.0";
    private string _changelog = "";
    private Vector2 _scroll;

    [MenuItem("Tools/Monetization/Release (Export + Changelog)", priority = 10)]
    public static void Open()
    {
        var w = GetWindow<MonetizationReleaseWindow>(true, "Monetization Release");
        w.minSize = new Vector2(540, 480);
        w.LoadState();
        w.Show();
    }

    private void LoadState()
    {
        if (File.Exists(VersionFile))
            _version = SuggestNextVersion(File.ReadAllText(VersionFile).Trim());
    }

    /// <summary>Gợi ý version kế tiếp: tăng patch (1.0.0 -> 1.0.1). Không parse được thì giữ nguyên.</summary>
    private static string SuggestNextVersion(string cur)
    {
        if (string.IsNullOrEmpty(cur)) return "1.0.0";
        var parts = cur.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[2], out int patch))
            return $"{parts[0]}.{parts[1]}.{patch + 1}";
        return cur;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Đóng gói bản phát hành", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Bấm Export sẽ:\n" +
            "• Tạo Assets/Release/_Monetization_v<version>.unitypackage\n" +
            "• Ghi version.txt + CHANGELOG_LATEST.md và cộng dồn vào CHANGELOG.md\n" +
            "Sau đó bạn commit + push. CI tự tạo GitLab Release theo version mới.",
            MessageType.Info);

        EditorGUILayout.Space();
        _version = EditorGUILayout.TextField("Version", _version);

        EditorGUILayout.LabelField("Changelog (mô tả thay đổi bản này):");
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
        _changelog = EditorGUILayout.TextArea(_changelog, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_version)))
        {
            if (GUILayout.Button($"📦 Export & Stage  v{_version}", GUILayout.Height(36)))
                DoExport();
        }
    }

    private void DoExport()
    {
        var version = _version.Trim();
        var changelog = string.IsNullOrWhiteSpace(_changelog) ? "- (chưa ghi changelog)" : _changelog.Trim();

        if (!AssetDatabase.IsValidFolder(ModuleFolder))
        {
            EditorUtility.DisplayDialog("Release", $"Không tìm thấy {ModuleFolder}.", "OK");
            return;
        }

        Directory.CreateDirectory(ReleaseFolderAbs);

        // 0) Xóa các bản export .unitypackage cũ (chỉ giữ bản mới nhất trong Assets/Release).
        //    Link tải của Release cũ trỏ vào file tại tag cũ trong lịch sử git nên không bị ảnh hưởng.
        DeleteOldPackages();

        // 1) Export .unitypackage (chỉ folder _Monetization, không kéo SDK).
        var fileName = $"_Monetization_v{version}.unitypackage";
        var pkgPath = Path.Combine(ReleaseFolderAbs, fileName);
        AssetDatabase.ExportPackage(ModuleFolder, pkgPath, ExportPackageOptions.Recurse);

        // 2) version.txt — CI đọc để lấy version + dựng tag/tên file.
        File.WriteAllText(VersionFile, version);

        // 3) CHANGELOG_LATEST.md — nội dung mô tả cho Release lần này (CI dùng làm description).
        File.WriteAllText(ChangelogLatest, changelog);

        // 4) CHANGELOG.md — lịch sử cộng dồn cho người đọc trong repo.
        PrependChangelog(version, changelog);

        AssetDatabase.Refresh();
        Debug.Log($"[MonetizationRelease] Đã stage v{version}: {pkgPath}");

        EditorUtility.DisplayDialog("Release đã sẵn sàng",
            $"Đã tạo:\n" +
            $"• Assets/Release/{fileName}\n" +
            $"• version.txt = {version}\n" +
            $"• CHANGELOG_LATEST.md + CHANGELOG.md\n\n" +
            $"Bước tiếp theo — commit & push:\n" +
            $"  git add Assets/Release\n" +
            $"  git commit -m \"release: v{version}\"\n" +
            $"  git push\n\n" +
            $"CI sẽ tự tạo Release v{version} khi version.txt thay đổi.",
            "OK");
    }

    /// <summary>Xóa mọi _Monetization_v*.unitypackage cũ (kèm .meta) trong Assets/Release.</summary>
    private static void DeleteOldPackages()
    {
        if (!Directory.Exists(ReleaseFolderAbs)) return;
        foreach (var f in Directory.GetFiles(ReleaseFolderAbs, "_Monetization_v*.unitypackage"))
        {
            File.Delete(f);
            var meta = f + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
            Debug.Log($"[MonetizationRelease] Đã xóa bản export cũ: {Path.GetFileName(f)}");
        }
    }

    private static void PrependChangelog(string version, string changelog)
    {
        var entry = new StringBuilder()
            .Append("## v").Append(version).Append(" — ").Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("\n\n")
            .Append(changelog).Append("\n\n")
            .ToString();

        const string title = "# Changelog\n\n";
        string body = "";
        if (File.Exists(ChangelogFull))
        {
            var existing = File.ReadAllText(ChangelogFull);
            body = existing.StartsWith(title) ? existing.Substring(title.Length) : existing;
        }
        File.WriteAllText(ChangelogFull, title + entry + body);
    }
}
#endif

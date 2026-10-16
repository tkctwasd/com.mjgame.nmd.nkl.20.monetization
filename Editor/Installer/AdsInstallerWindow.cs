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

public partial class AdsInstallerWindow : EditorWindow
{
    static readonly string[] Tabs = { "Firebase", "AppLovin", "AppsFlyer", "Adjust", "IAP", "Review" };

    const int IapTab = 4;

    readonly string[] labels = new string[Tabs.Length];
    int tab;
    FirebaseInstallerTab firebase;
    SdkInstallerTab[] sdks;
    IapInstallerTab iap;

    // Tab SDK nam truoc va sau tab IAP
    SdkInstallerTab Sdk(int t) => sdks[t < IapTab ? t - 1 : t - 2];

    [MenuItem("Tools/Monetization/Ads Installer")]
    static void Open()
    {
        // Moi lan mo tu menu deu bat dau o tab Firebase
        SessionState.SetInt("AdsInstaller.Tab", 0);
        var window = GetWindow<AdsInstallerWindow>();
        window.minSize = new Vector2(600, 520);
        window.tab = 0;
        window.EnableTab();

        // Giua cua so Unity, du cao de thay het 12 module Firebase khong phai cuon
        var main = EditorGUIUtility.GetMainWindowPosition();
        var size = new Vector2(Mathf.Max(520, window.position.width), Mathf.Min(640, main.height - 40));
        window.position = new Rect(main.center - size / 2, size);

        var setInfo = GetWindow<SetInfoWindow>(typeof(AdsInstallerWindow));
        setInfo.Show();
        window.Focus();
    }

    Vector2 scroll;

    // Tieu de tab co icon de phan biet voi tab Set Info
    public static GUIContent Title(string text, string icon)
    {
        var c = EditorGUIUtility.IconContent(icon);
        return new GUIContent(" " + text, c?.image, text);
    }

    void OnEnable()
    {
        titleContent = Title("1. Ads Installer", "d_Package Manager");
        firebase = new FirebaseInstallerTab(this);
        iap = new IapInstallerTab(this);
        sdks = new SdkInstallerTab[] { new AppLovinInstallerTab(this), new AppsFlyerInstallerTab(this), new AdjustInstallerTab(this), new ReviewInstallerTab(this) };
        // Adjust va AppsFlyer la 2 MMP, chi duoc cai 1
        sdks[1].ExclusiveWith = sdks[2];
        sdks[2].ExclusiveWith = sdks[1];
        tab = SessionState.GetInt("AdsInstaller.Tab", 0);
        // Quet tat ca SDK de danh dau tab da cai
        firebase.Detect();
        foreach (var s in sdks) s.Detect();
        SetupAppLovinExtras();
        iap.Detect();
        EnableTab();
    }

    void OnDisable()
    {
        firebase.Disable();
        foreach (var s in sdks) s.Disable();
        iap.Disable();
    }

    const string PendingKey = "AdsInstaller.Pending";
    string doneMessage;

    // Thao tac dang cho Unity compile/import xong; luu SessionState de con sau domain reload
    public static void SetPending(string action)
    {
        SessionState.SetString(PendingKey, action);
        SessionState.SetFloat(PendingKey + ".Time", (float)EditorApplication.timeSinceStartup);
    }

    void OnInspectorUpdate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || SessionState.GetString(PendingKey, "").Length > 0) Repaint();
    }

    // true khi dang compile/import: ve thanh tien trinh thay cho noi dung tab
    bool DrawPending()
    {
        var pending = SessionState.GetString(PendingKey, "");
        var working = EditorApplication.isCompiling || EditorApplication.isUpdating;
        if (working)
        {
            var what = EditorApplication.isCompiling ? "Unity dang compile" : "Unity dang import asset";
            var label = pending.Length > 0 ? $"Dang {pending}... ({what})" : $"{what}...";
            // Unity khong cho biet % compile nen thanh chay qua lai
            var t = (float)(EditorApplication.timeSinceStartup % 2.0) / 2f;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 22), Mathf.PingPong(t * 2f, 1f), label);
            EditorGUILayout.HelpBox("Doi Unity compile xong roi moi thao tac tiep.", MessageType.Info);
            return true;
        }
        // Cho vai giay de Unity kip bat dau compile/import sau thao tac
        var age = EditorApplication.timeSinceStartup - SessionState.GetFloat(PendingKey + ".Time", 0f);
        if (pending.Length > 0 && age < 3)
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 22), 0f, $"Dang {pending}...");
            return true;
        }
        if (pending.Length > 0)
        {
            SessionState.EraseString(PendingKey);
            doneMessage = $"Xong: {pending}";
            firebase.Detect();
            foreach (var s in sdks) s.Detect();
            iap.Detect();
            EnableTab();
        }
        if (doneMessage != null)
        {
            GUI.backgroundColor = new Color(0.4f, 1f, 0.4f);
            EditorGUILayout.HelpBox(doneMessage, MessageType.Info);
            GUI.backgroundColor = Color.white;
        }
        return false;
    }

    void OnGUI()
    {
        for (var i = 0; i < Tabs.Length; i++)
        {
            var on = i == 0 ? firebase.HasInstalled : i == IapTab ? iap.HasInstalled : Sdk(i).HasInstalled;
            labels[i] = on ? $"✓ {Tabs[i]}" : Tabs[i];
        }
        var newTab = GUILayout.Toolbar(tab, labels, GUILayout.Height(24));
        if (newTab != tab)
        {
            tab = newTab;
            SessionState.SetInt("AdsInstaller.Tab", tab);
            EnableTab();
        }
        EditorGUILayout.Space();
        if (DrawPending()) return;
        // Chi cuon ca cua so khi cua so nho hon noi dung
        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (tab == 0) firebase.Draw();
        else if (tab == IapTab) iap.Draw();
        else Sdk(tab).Draw();
        EditorGUILayout.EndScrollView();
    }

    // Danh sach Mediated Networks cua MAX, chi co khi bat USE_APPLOVIN (MaxNetworksPanel.cs)
    partial void SetupAppLovinExtras();

    void EnableTab()
    {
        if (tab == 0) firebase.Enable();
        else if (tab == IapTab) iap.Enable();
        else Sdk(tab).Enable();
    }
}

#endif

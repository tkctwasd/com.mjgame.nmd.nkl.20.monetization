using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class AdsTestSceneBuilder
{
    private const string Root = "Assets/_TEST";
    private const string ScenePath = Root + "/Scenes/AdsTestScene.unity";
    private const string ConstantsPath = Root + "/AdConstants_Test.asset";
    private const string ProductsPath = Root + "/IAPProducts_Test.asset";

    [MenuItem("Tools/Ads/Build Test Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(Root + "/Scenes");
        CopyRemoteConfigPerPlatform();

        var constants = AssetDatabase.LoadAssetAtPath<AdConstants>(ConstantsPath);
        if (constants == null)
        {
            constants = ScriptableObject.CreateInstance<AdConstants>();
            AssetDatabase.CreateAsset(constants, ConstantsPath);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Môi trường
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        cam.transform.SetPositionAndRotation(new Vector3(0, 4, -8), Quaternion.Euler(20, 0, 0));
        cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        cam.GetComponent<Camera>().backgroundColor = new Color(0.12f, 0.14f, 0.2f);

        var light = new GameObject("Directional Light", typeof(Light));
        light.GetComponent<Light>().type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3, 1, 3);

        var player = GameObject.CreatePrimitive(PrimitiveType.Cube);
        player.name = "Player";
        player.transform.position = new Vector3(0, 0.5f, 0);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // Managers giống game thật
        var managers = new GameObject("--- Managers ---");
        AddChild<FirebaseManager>(managers, "FirebaseManager");
        AddChild<FirebaseAnalyticManager>(managers, "FirebaseAnalyticManager");
        AddChild<RemoteConfigManager>(managers, "RemoteConfigManager");
#if USE_ADJUST
        AddChild<AdjustManager>(managers, "AdjustManager");
#endif
#if USE_APPSFLYER
        AddChild<AppsFlyerManager>(managers, "AppsFlyerManager");
#endif
        BuildUIManager();

        var adsGo = new GameObject("AdsManager");
        adsGo.SetActive(false); // gán AdConstants trước khi Awake chạy
        adsGo.transform.SetParent(managers.transform);
        adsGo.AddComponent<AdsManager>().AdConstants = constants;
        adsGo.SetActive(true);
#if USE_IAP
        var products = AssetDatabase.LoadAssetAtPath<IAPProducts>(ProductsPath);
        if (products == null)
        {
            products = ScriptableObject.CreateInstance<IAPProducts>();
            AssetDatabase.CreateAsset(products, ProductsPath);
        }
        var iapGo = new GameObject("IAPManager");
        iapGo.SetActive(false);
        iapGo.transform.SetParent(managers.transform);
        iapGo.AddComponent<IAPManager>().IAPProducts = products;
        iapGo.SetActive(true);
#endif
        AddChild<MonetizationBridge>(managers, "MonetizationBridge");

        var controller = new GameObject("AdsTestController").AddComponent<AdsTestController>();
        controller.Player = player.transform;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Selection.activeObject = constants;
        Debug.Log($"<color=green><b>Ads test scene created: {ScenePath}</b></color> — điền Ad Unit ID trong {ConstantsPath} rồi bấm Apply Ad Ids.");
    }

    // UIManager (package com.mjgame) cần mainCanvas + panelParent, panel phải có CanvasGroup
    private static void BuildUIManager()
    {
        var canvasGo = new GameObject("UIManager", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasGo.SetActive(false); // gán field trước khi Awake chạy
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var panelParent = new GameObject("Panels", typeof(RectTransform));
        panelParent.transform.SetParent(canvasGo.transform, false);
        Stretch(panelParent.GetComponent<RectTransform>());

        var resume = new GameObject("PanelResume", typeof(RectTransform), typeof(CanvasGroup));
        resume.transform.SetParent(panelParent.transform, false);
        Stretch(resume.GetComponent<RectTransform>());
        resume.GetComponent<CanvasGroup>().blocksRaycasts = false; // Complete() chưa triển khai, tránh chặn input
        resume.AddComponent<PanelResume>();

        var ui = canvasGo.AddComponent<UIManager>();
        var so = new SerializedObject(ui);
        so.FindProperty("mainCanvas").objectReferenceValue = canvas;
        so.FindProperty("panelParent").objectReferenceValue = panelParent.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        canvasGo.SetActive(true);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void AddChild<T>(GameObject parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.AddComponent<T>();
    }

    // RemoteConfigManager đọc RemoteConfig_Android / RemoteConfig_iOS theo platform
    private static void CopyRemoteConfigPerPlatform()
    {
        string src = Root + "/Resources/RemoteConfig.json";
        if (!File.Exists(src)) return;
        foreach (var suffix in new[] { "_Android", "_iOS" })
        {
            string dst = Root + $"/Resources/RemoteConfig{suffix}.json";
            if (!File.Exists(dst)) File.Copy(src, dst);
        }
    }
}

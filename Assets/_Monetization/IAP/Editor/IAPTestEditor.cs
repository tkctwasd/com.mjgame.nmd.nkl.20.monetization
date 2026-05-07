#if USE_IAP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Editor window để test toàn bộ IAP flow trong Editor Play Mode và trên device.
/// Menu: Tools/Monetization/IAP Test Window
/// </summary>
public class IAPTestEditor : EditorWindow
{
    // ── Colors ───────────────────────────────────────────────────────────────
    static readonly Color ColPass = new(0.35f, 0.85f, 0.35f);
    static readonly Color ColFail = new(0.90f, 0.30f, 0.30f);
    static readonly Color ColPending = new(0.90f, 0.80f, 0.30f);
    static readonly Color ColInfo = new(0.60f, 0.85f, 1.00f);

    enum Status { Pending, Pass, Fail }

    class TestCase
    {
        public string Category;
        public string Name;
        public string Steps;
        public string Expected;
        public Status Status = Status.Pending;
        public string Notes = "";
    }

    // ── State ────────────────────────────────────────────────────────────────
    readonly List<TestCase> cases = new();
    readonly List<(string t, string msg, LogType type)> log = new();

    int tab;
    readonly string[] tabNames = { "State", "Test Cases", "Actions", "Simulate", "Log" };

    string actProductId = "";
    string simProductId = "";
    string filterCat = "All";
    string[] catOptions;

    Vector2 scrollMain;
    Vector2 scrollLog;
    bool subscribedEvents;
    double nextRepaint;

    // ── Open ─────────────────────────────────────────────────────────────────
    [MenuItem("Tools/IAP/IAP Test")]
    public static void Open() => GetWindow<IAPTestEditor>("IAP Tester");

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        BuildCases();
        Application.logMessageReceived += CaptureUnityLog;
        EditorApplication.playModeStateChanged += OnPlayMode;
        TrySubscribe();
    }

    void OnDisable()
    {
        Application.logMessageReceived -= CaptureUnityLog;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        subscribedEvents = false;
    }

    void OnPlayMode(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.delayCall += () => { TrySubscribe(); Repaint(); };
        else if (s == PlayModeStateChange.ExitingPlayMode)
            subscribedEvents = false;
    }

    void TrySubscribe()
    {
        if (!Application.isPlaying || subscribedEvents) return;
        var m = IAPManager.Instance;
        if (m == null) return;

        m.OnProductsReady += p => { AddLog($"[EVENT] OnProductsReady — {p.Count} products"); Repaint(); };
        m.OnProductsFetchFailed += () => { AddLog("[EVENT] OnProductsFetchFailed", LogType.Warning); Repaint(); };
        m.OnPurchasesReady += o => { AddLog($"[EVENT] OnPurchasesReady — Confirmed:{o.ConfirmedOrders.Count} Pending:{o.PendingOrders.Count}"); Repaint(); };
        m.OnProductDelivered += (id, _) => { AddLog($"[EVENT] OnProductDelivered — {id}"); Repaint(); };
        m.OnPurchaseFailedEvent += f => { AddLog($"[EVENT] OnPurchaseFailedEvent — {f?.FailureReason}", LogType.Warning); Repaint(); };
        m.OnRestoreComplete += ok => { AddLog($"[EVENT] OnRestoreComplete — {(ok ? "success" : "fail")}", ok ? LogType.Log : LogType.Warning); Repaint(); };

        subscribedEvents = true;
        AddLog("[IAPTestWindow] Subscribed to IAPManager events");
    }

    // ── GUI ──────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        tab = GUILayout.Toolbar(tab, tabNames, EditorStyles.toolbarButton);
        EditorGUILayout.EndHorizontal();

        scrollMain = EditorGUILayout.BeginScrollView(scrollMain);
        switch (tab)
        {
            case 0: DrawState(); break;
            case 1: DrawTestCases(); break;
            case 2: DrawActions(); break;
            case 3: DrawSimulate(); break;
            case 4: DrawLog(); break;
        }
        EditorGUILayout.EndScrollView();

        if (Application.isPlaying && EditorApplication.timeSinceStartup > nextRepaint)
        {
            nextRepaint = EditorApplication.timeSinceStartup + 0.5;
            Repaint();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1 — STATE
    // ═══════════════════════════════════════════════════════════════════════════
    void DrawState()
    {
        Space();
        Bold("IAP Manager State");

        if (!NeedPlayMode()) return;
        var m = NeedManager(); if (m == null) return;

        BoolRow("IsConnected", m.IsConnected);
        BoolRow("IsProductsReady", m.IsProductsReady);
        BoolRow("IsPurchasesReady", m.IsPurchasesReady);

        Space(8);
        Bold("Fetched Products");

        var products = m.PurchaseModule?.GetFetchedProducts();
        if (products == null || products.Count == 0)
        {
            EditorGUILayout.LabelField("— none fetched yet —", EditorStyles.miniLabel);
            return;
        }

        foreach (var p in products)
        {
            bool owned = m.PurchaseModule.IsPurchased(p.definition.id);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(p.definition.id, GUILayout.Width(220));
            EditorGUILayout.LabelField(p.metadata?.localizedPriceString ?? "—", GUILayout.Width(80));
            EditorGUILayout.LabelField(p.definition.type.ToString(), GUILayout.Width(110));
            Tint(owned ? "OWNED" : "not owned", owned ? ColPass : Color.gray);
            EditorGUILayout.EndHorizontal();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2 — TEST CASES
    // ═══════════════════════════════════════════════════════════════════════════
    void DrawTestCases()
    {
        if (catOptions == null || catOptions.Length == 0)
            RebuildCatOptions();

        // Safety fallback
        if (catOptions == null || catOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("No category options found.", MessageType.Warning);
            return;
        }

        Space();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Category:", GUILayout.Width(62));

        int ci = Array.IndexOf(catOptions, filterCat);

        // Clamp index an toàn
        if (ci < 0 || ci >= catOptions.Length)
            ci = 0;

        ci = GUILayout.Toolbar(ci, catOptions);

        // Safety sau Toolbar
        if (ci >= 0 && ci < catOptions.Length)
            filterCat = catOptions[ci];
        else
            filterCat = catOptions[0];

        EditorGUILayout.EndHorizontal();

        Space(2);

        int pass = cases.Count(t => t.Status == Status.Pass);
        int fail = cases.Count(t => t.Status == Status.Fail);
        int pend = cases.Count(t => t.Status == Status.Pending);

        EditorGUILayout.BeginHorizontal();

        Tint($"✓ {pass} Pass", ColPass);
        Tint($"✗ {fail} Fail", ColFail);
        Tint($"◌ {pend} Pending", ColPending);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Reset All", GUILayout.Width(76)))
        {
            foreach (var t in cases)
            {
                t.Status = Status.Pending;
                t.Notes = "";
            }
        }

        EditorGUILayout.EndHorizontal();

        Space(4);

        string lastCat = "";

        foreach (var tc in cases)
        {
            if (filterCat != "All" && tc.Category != filterCat)
                continue;

            if (tc.Category != lastCat)
            {

                Space(6);

                EditorGUILayout.LabelField(
                    $"── {tc.Category.ToUpper()} ──────────────────────────",
                    EditorStyles.boldLabel);

                lastCat = tc.Category;
            }

            DrawCase(tc);
        }
    }

    void DrawCase(TestCase tc)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        var ns = new GUIStyle(EditorStyles.boldLabel);
        ns.normal.textColor = tc.Status switch
        {
            Status.Pass => ColPass,
            Status.Fail => ColFail,
            _ => Color.white
        };
        EditorGUILayout.LabelField(tc.Name, ns);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✓", GUILayout.Width(24))) tc.Status = Status.Pass;
        if (GUILayout.Button("✗", GUILayout.Width(24))) tc.Status = Status.Fail;
        if (GUILayout.Button("◌", GUILayout.Width(24))) tc.Status = Status.Pending;
        EditorGUILayout.EndHorizontal();

        var wrap = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        EditorGUILayout.LabelField("▶ Steps: " + tc.Steps, wrap);
        var boldWrap = new GUIStyle(wrap) { fontStyle = FontStyle.Bold };
        EditorGUILayout.LabelField("✓ Expected: " + tc.Expected, boldWrap);

        tc.Notes = EditorGUILayout.TextField("Notes:", tc.Notes);
        EditorGUILayout.EndVertical();
        Space(2);
    }

    void RebuildCatOptions()
    {
        var cats = new List<string> { "All" };
        cats.AddRange(cases.Select(t => t.Category).Distinct());
        catOptions = cats.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3 — ACTIONS (real store calls)
    // ═══════════════════════════════════════════════════════════════════════════
    void DrawActions()
    {
        Space();
        Bold("Real Store Actions");
        EditorGUILayout.HelpBox("Triggers actual IAP calls. Cần sandbox account và mạng.", MessageType.Info);

        if (!NeedPlayMode()) return;
        var m = NeedManager(); if (m == null) return;

        Space(6);
        EditorGUILayout.LabelField("Purchase by ID", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        actProductId = EditorGUILayout.TextField(actProductId);
        GUI.enabled = !string.IsNullOrWhiteSpace(actProductId);
        if (GUILayout.Button("Purchase", GUILayout.Width(80)))
            m.PurchaseModule.Purchase(actProductId, (id, ok) =>
                AddLog($"Purchase({id}) → {(ok ? "SUCCESS" : "FAIL")}", ok ? LogType.Log : LogType.Warning));
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        var products = m.PurchaseModule?.GetFetchedProducts();
        if (products != null && products.Count > 0)
        {
            Space(4);
            EditorGUILayout.LabelField("Quick Purchase from store", EditorStyles.miniBoldLabel);
            foreach (var p in products)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(p.definition.id, GUILayout.Width(200));
                EditorGUILayout.LabelField(p.metadata?.localizedPriceString ?? "—", GUILayout.Width(70));
                if (GUILayout.Button("Buy", GUILayout.Width(46)))
                    m.PurchaseModule.Purchase(p.definition.id, (id, ok) =>
                        AddLog($"Purchase({id}) → {(ok ? "SUCCESS" : "FAIL")}", ok ? LogType.Log : LogType.Warning));
                EditorGUILayout.EndHorizontal();
            }
        }

        Space(8);
        EditorGUILayout.LabelField("Restore", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Restore Purchases"))
            m.PurchaseModule.RestorePurchases(ok =>
                AddLog($"RestorePurchases → {(ok ? "SUCCESS" : "FAIL")}", ok ? LogType.Log : LogType.Warning));

        Space(4);
        EditorGUILayout.LabelField("Fetch Existing Purchases", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Fetch Purchases"))
            m.PurchaseModule.FetchExistingPurchases();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 4 — SIMULATE (no real store)
    // ═══════════════════════════════════════════════════════════════════════════
    void DrawSimulate()
    {
        Space();
        Bold("Simulate Callbacks");
        EditorGUILayout.HelpBox(
            "Kích hoạt trực tiếp các internal methods trên IAPManager để test UI mà không cần store.\n" +
            "PendingOrder / FailedOrder là sealed type → không thể giả hoàn toàn.\n" +
            "Dùng sandbox account thật cho các case purchase flow.",
            MessageType.Info);

        if (!NeedPlayMode()) return;
        var m = NeedManager(); if (m == null) return;

        Space(6);

        // ── Product Fetch Fail ────────────────────────────────────────────────
        EditorGUILayout.LabelField("Product Fetch", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("▶ Simulate OnProductsFetchFailed  (no network)"))
        {
            Invoke(m, "NotifyProductsFetchFailed");
            AddLog("[SIM] NotifyProductsFetchFailed → tất cả IAP buttons phải giữ loadingOverlay=ON", LogType.Warning);
        }

        Space(6);

        // ── Purchase Success (UI only) ────────────────────────────────────────
        EditorGUILayout.LabelField("Purchase Delivered (UI simulation)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Product ID:", GUILayout.Width(72));
        simProductId = EditorGUILayout.TextField(simProductId);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("▶ Simulate DeliverProduct  (success → overlay hide)"))
        {
            if (string.IsNullOrWhiteSpace(simProductId))
                AddLog("[SIM] Product ID trống, điền ID trước", LogType.Warning);
            else
            {
                Invoke(m, "DeliverProduct", simProductId, (ConfirmedOrder)null);
                AddLog($"[SIM] DeliverProduct({simProductId}) → PanelOverlay + loadingOverlay phải tắt");
            }
        }

        Space(6);

        // ── PanelOverlay manual ───────────────────────────────────────────────
        EditorGUILayout.LabelField("PanelOverlay Manual Toggle", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Show PanelOverlay"))
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowPanel<PanelOverlay>();
            AddLog("[SIM] ShowPanel<PanelOverlay>");
        }
        if (GUILayout.Button("Hide PanelOverlay"))
        {
            if (UIManager.Instance != null) UIManager.Instance.HidePanel<PanelOverlay>();
            AddLog("[SIM] HidePanel<PanelOverlay>");
        }
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 5 — LOG
    // ═══════════════════════════════════════════════════════════════════════════
    void DrawLog()
    {
        Space();
        EditorGUILayout.BeginHorizontal();
        Bold("Event Log  (mới nhất ở trên)");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear", GUILayout.Width(54))) log.Clear();
        EditorGUILayout.EndHorizontal();

        scrollLog = EditorGUILayout.BeginScrollView(scrollLog, GUILayout.MinHeight(420));
        var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        for (int i = log.Count - 1; i >= 0; i--)
        {
            var (t, msg, type) = log[i];
            var old = GUI.color;
            GUI.color = type == LogType.Error ? ColFail :
                        type == LogType.Warning ? ColPending : ColInfo;
            EditorGUILayout.LabelField($"[{t}] {msg}", style);
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEST CASE LIST
    // ═══════════════════════════════════════════════════════════════════════════
    void BuildCases()
    {
        cases.Clear();

        // ── 1. Initialization ─────────────────────────────────────────────────
        TC("Init", "1.1 Unity Services Init — Thành công",
            "Khởi động game với mạng, sandbox account hợp lệ.",
            "Log 'Store Connected!'. IsConnected=true. IsProductsReady=true sau vài giây.");

        TC("Init", "1.2 Unity Services Init — Không có mạng",
            "Tắt WiFi/Data TRƯỚC khi mở game.",
            "Log error 'Unity Services failed'. IsConnected=false. Không crash. IAP buttons giữ loadingOverlay=ON.");

        TC("Init", "1.3 iOS — Sandbox account chưa sign in / chưa đồng ý T&C",
            "Dùng sandbox account chưa accept sandbox T&C trên thiết bị iOS.",
            "Init thành công nhưng fetch product fail hoặc trả empty. Buttons giữ loadingOverlay=ON.");

        TC("Init", "1.4 Android — Google Play chưa sign in",
            "Thiết bị Android chưa đăng nhập Google Play account.",
            "StoreDisconnected hoặc ProductsFetchFailed. Không crash.");

        // ── 2. Store Connection ───────────────────────────────────────────────
        TC("Store", "2.1 Store Connect — Thành công",
            "Khởi động game bình thường có mạng.",
            "Log 'Store Connected!'. IsConnected=true.");

        TC("Store", "2.2 Mất mạng SAU KHI đã connect",
            "Connect thành công → tắt mạng → tiếp tục dùng app → nhấn nút mua.",
            "OnStoreDisconnected fired. IsConnected=false. Purchase() log warning 'Not ready', không crash.");

        TC("Store", "2.3 Mất mạng trong lúc đang fetch product",
            "Bật game → tắt mạng ngay khi thấy log 'FetchProducts requested'.",
            "OnProductsFetchFailed. Tất cả IAP buttons giữ loadingOverlay=ON. Không crash.");

        TC("Store", "2.4 Bật lại mạng sau khi đã mất",
            "Mất mạng → OnStoreDisconnected → bật lại mạng.",
            "Nếu SDK tự retry: IsConnected=true trở lại. Nếu không: cần restart app (thông báo user).");

        // ── 3. Product Fetch ──────────────────────────────────────────────────
        TC("Product", "3.1 Fetch — Tất cả product ID đúng",
            "Product IDs trong IAPProducts config phải trùng với store console (App Store Connect / Google Play Console).",
            "IsProductsReady=true. Mỗi IAPButtonUI hiển thị đúng tên + giá localizedPriceString. loadingOverlay tắt.");

        TC("Product", "3.2 Fetch — Một product ID không tồn tại trong store",
            "Đổi 1 product ID thành chuỗi sai (VD: 'com.test.wrong_id'), chạy lại.",
            "FindProduct(id) trả null. Button đó giữ loadingOverlay=ON (blocked). Các button khác bình thường.");

        TC("Product", "3.3 Fetch — Tất cả fail (không mạng)",
            "Tắt mạng hoàn toàn trước khi game connect store.",
            "OnProductsFetchFailed fired. Tất cả IAP buttons loadingOverlay=ON. Không crash app.");

        TC("Product", "3.4 Fetch — Catalog rỗng (0 product trong config)",
            "Xóa tất cả product khỏi IAPProducts config, chạy lại.",
            "IsProductsReady=true nhưng products.Count=0. Không crash.");

        TC("Product", "3.5 Button Start TRƯỚC khi products ready",
            "Quan sát button ngay khi game start (bình thường).",
            "Awake: loadingOverlay=ON → OnProductsReady → RefreshUI → loadingOverlay=OFF. Giá hiển thị đúng.");

        TC("Product", "3.6 Button OnEnable SAU KHI products đã ready",
            "Hide panel chứa IAP buttons → đợi IsProductsReady=true → Show panel lại.",
            "Button.Start() check IsProductsReady=true ngay lập tức, gọi RefreshUI(). loadingOverlay tắt, giá đúng.");

        TC("Product", "3.7 Giá hiển thị đúng theo region device",
            "Test trên device với region khác nhau (US, VN, JP...).",
            "localizedPriceString hiển thị đúng currency + format theo region của device.");

        // ── 4. Purchase Flow ──────────────────────────────────────────────────
        TC("Purchase", "4.1 Mua Consumable — Thành công",
            "Nhấn nút mua consumable. Xác nhận trong sandbox payment dialog.",
            "Click: PanelOverlay hiện + loadingOverlay=ON, button blocked.\nSau confirm: PanelOverlay tắt + loadingOverlay=OFF.\nOnProductDelivered fired. LogPurchaseBtnClick + LogPurchaseAttempt ghi vào Firebase.");

        TC("Purchase", "4.2 Mua Non-Consumable — Thành công lần đầu",
            "Nhấn mua non-consumable (VD: Remove Ads). Xác nhận.",
            "Giống 4.1. Sau delivered: PurchaseModule.IsPurchased(id)=true.\nOnPurchaseSuccess override được gọi trong subclass.");

        TC("Purchase", "4.3 Mua Non-Consumable — Đã sở hữu (duplicate purchase)",
            "Mua lại non-consumable đã có, xác nhận.",
            "Store tự báo 'already owned' → OnPurchasePending → ConfirmPurchase → OnProductDelivered.\nKhông charge tiền lần 2. PanelOverlay + overlay tắt đúng.");

        TC("Purchase", "4.4 User Cancel — Đóng payment dialog",
            "Nhấn mua → payment dialog hiện → bấm Cancel / Back / ngoài vùng.",
            "OnPurchaseFailedEvent, FailureReason=UserCancelled.\nPanelOverlay tắt. loadingOverlay=OFF. Log warning (không phải error).");

        TC("Purchase", "4.5 Payment Declined — Thẻ bị từ chối",
            "iOS sandbox: dùng card test bị decline. Android: dùng test payment bị từ chối.",
            "OnPurchaseFailedEvent, FailureReason=PaymentDeclined.\nPanelOverlay + loadingOverlay tắt. LogPurchaseFailure gửi Firebase.");

        TC("Purchase", "4.6 Network error giữa chừng payment dialog",
            "Nhấn mua → tắt mạng → thử xác nhận thanh toán.",
            "OnPurchaseFailedEvent hoặc OnPurchaseDeferred.\nQuan trọng: PanelOverlay PHẢI tắt, không bị treo vĩnh viễn.");

        TC("Purchase", "4.7 Purchase Deferred — iOS Ask to Buy / Parental Controls",
            "Thiết bị iOS Family Sharing, child account cần parent approve.",
            "OnPurchaseDeferred fired. PanelOverlay tắt ngay (game không bị lock).\nSau parent approve qua email: OnPurchasePending → AutoConfirm → OnProductDelivered.");

        TC("Purchase", "4.8 Mua khi CanPurchase=false (store chưa connect)",
            "Tắt mạng → đợi OnStoreDisconnected → nhấn nút mua.",
            "PurchaseModule.Purchase() log warning 'Not ready. Purchase skipped'. Không crash.\nPanelOverlay không hiện (Purchase() return sớm trước BeginPurchaseFlow).");

        TC("Purchase", "4.9 Double-click nút mua trong < 200ms",
            "Nhấn nút mua 2 lần thật nhanh (cả 2 ngón tay).",
            "Chỉ 1 lần purchase trigger: button.interactable=false sau click đầu.\nPanelOverlay che màn hình ngăn click thứ 2.");

        TC("Purchase", "4.10 Tap button khác trong khi PanelOverlay đang show",
            "Click mua → PanelOverlay hiện → tap vào bất kỳ nút nào phía sau.",
            "PanelOverlay phải block toàn bộ touch/click (verify GraphicRaycaster trên overlay canvas).\nKhông có hành động nào trigger.");

        TC("Purchase", "4.11 App vào background trong payment dialog",
            "Nhấn mua → Home button → đợi 5 giây → quay lại app.",
            "PanelOverlay + loadingOverlay vẫn đúng trạng thái khi resume.\nKhi store callback về: overlay tắt đúng.");

        TC("Purchase", "4.12 Kill app giữa payment, restart",
            "Force-quit app khi payment dialog đang mở → mở lại app.",
            "Nếu payment đã charge: OnPurchasesReady chứa order → auto deliver (không mất hàng, không charge 2 lần).\nNếu chưa charge: không có order mới.");

        TC("Purchase", "4.13 Mua Subscription — Thành công",
            "Nhấn mua subscription product. Xác nhận.",
            "OnProductDelivered fired. IsPurchased(id)=true trong thời gian còn hạn.");

        TC("Purchase", "4.14 Subscription hết hạn",
            "Để subscription hết hạn (iOS sandbox: thời gian accelerated), restart app.",
            "IsPurchased(id)=false. OnPurchasesReady không chứa order đó nữa.");

        // ── 5. Restore ────────────────────────────────────────────────────────
        TC("Restore", "5.1 Restore — Có purchases sau reinstall",
            "Reinstall app. Sign in đúng account cũ. Gọi RestorePurchases().",
            "OnRestoreComplete(true). OnProductDelivered fired cho mỗi non-consumable. IsPurchased(id)=true trở lại.");

        TC("Restore", "5.2 Restore — Account chưa mua gì",
            "Gọi RestorePurchases() với account mới, chưa purchase.",
            "OnRestoreComplete(true). Không có OnProductDelivered. Hiện thông báo phù hợp (không có gì để restore).");

        TC("Restore", "5.3 Restore — Fail (mất mạng)",
            "Tắt mạng → gọi RestorePurchases().",
            "OnRestoreComplete(false). Log warning. Không crash.");

        TC("Restore", "5.4 iOS Restore — Yêu cầu sign in Apple ID",
            "Gọi RestorePurchases() khi chưa sign in App Store.",
            "iOS hiển thị sign-in dialog. Sau khi sign in: callback tiếp tục bình thường.");

        TC("Restore", "5.5 Android — Restore tự động khi FetchPurchases",
            "Android không có Restore API riêng. FetchExistingPurchases() phải trả về purchases cũ.",
            "OnPurchasesReady chứa ConfirmedOrders của các non-consumable đã mua. IsPurchased(id)=true.");

        // ── 6. UI State ───────────────────────────────────────────────────────
        TC("UI", "6.1 loadingOverlay ON khi đang chờ products",
            "Quan sát button từ lúc Awake đến khi OnProductsReady.",
            "loadingOverlay=ON trong suốt thời gian chờ. Khi products ready → OFF.");

        TC("UI", "6.2 loadingOverlay ON khi product ID không tìm thấy trong store",
            "Đặt ProductId sai trong IAPButtonUI inspector (1 button). Chạy bình thường.",
            "Button đó giữ loadingOverlay=ON. Button.interactable=false. Các button ID đúng hoạt động bình thường.");

        TC("UI", "6.3 loadingOverlay ON khi fetch products fail",
            "Tắt mạng → khởi động → observe tất cả IAP buttons.",
            "Tất cả buttons giữ loadingOverlay=ON. Không button nào interactable.");

        TC("UI", "6.4 PanelOverlay block toàn bộ input khi mua",
            "Nhấn mua → PanelOverlay hiện → thử tap khắp nơi trên màn hình.",
            "Không có event nào trigger từ các UI element phía sau. GraphicRaycaster trên PanelOverlay canvas phải blocking.");

        TC("UI", "6.5 PanelOverlay tắt sau success",
            "Mua thành công.",
            "PanelOverlay.Hide() được gọi. Panel không visible. Các button khác tương tác được trở lại.");

        TC("UI", "6.6 PanelOverlay tắt sau fail / cancel",
            "Cancel mua hoặc payment fail.",
            "PanelOverlay tắt. loadingOverlay=OFF. User có thể mua lại bình thường.");

        TC("UI", "6.7 PanelOverlay tắt sau deferred",
            "Trigger Ask to Buy (4.7).",
            "PanelOverlay tắt ngay khi OnPurchaseDeferred, không đợi parent approve.");

        // ── 7. Analytics ──────────────────────────────────────────────────────
        TC("Analytics", "7.1 LogPurchaseBtnClick khi click button",
            "Click nút mua. Check Firebase DebugView (firebase.google.com/analytics).",
            "Event 'iap_btn_click' với param 'product_id' xuất hiện ngay lập tức trong DebugView.");

        TC("Analytics", "7.2 LogPurchaseAttempt khi OnPurchasePending",
            "Xác nhận payment dialog. Check Firebase.",
            "Event 'iap_attempt' xuất hiện sau khi payment confirm.");

        TC("Analytics", "7.3 LogPurchaseFailure khi fail / cancel",
            "Cancel mua. Check Firebase.",
            "Event 'iap_fail' với failureReason=UserCancelled và product_id đúng.");

        TC("Analytics", "7.4 Revenue tracker gửi khi OnPurchaseConfirmed",
            "Mua thành công. Check Appsflyer / Adjust dashboard.",
            "Revenue event với giá trị + currency đúng. Không gửi double (chỉ 1 lần per TX).");

        TC("Analytics", "7.5 LogRestoreStart và LogRestoreComplete",
            "Gọi RestorePurchases(). Check Firebase.",
            "Event 'iap_restore_start' → sau đó 'iap_restore_complete' với param success=true/false.");
    }

    void TC(string cat, string name, string steps, string expected) =>
        cases.Add(new TestCase { Category = cat, Name = name, Steps = steps, Expected = expected });

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    bool NeedPlayMode()
    {
        if (Application.isPlaying) return true;
        EditorGUILayout.HelpBox("Enter Play Mode để sử dụng tab này.", MessageType.Warning);
        return false;
    }

    IAPManager NeedManager()
    {
        var m = IAPManager.Instance;
        if (m != null) return m;
        EditorGUILayout.HelpBox("IAPManager.Instance là null. Kiểm tra scene có IAPManager chưa.", MessageType.Error);
        return null;
    }

    void AddLog(string msg, LogType type = LogType.Log)
    {
        log.Add((DateTime.Now.ToString("HH:mm:ss.ff"), msg, type));
        if (log.Count > 600) log.RemoveAt(0);
    }

    void CaptureUnityLog(string cond, string _, LogType type)
    {
        if (cond.Contains("[IAP") || cond.Contains("IAPManager") ||
            cond.Contains("Purchase") || cond.Contains("Store") || cond.Contains("[SIM]"))
            AddLog(cond, type);
    }

    // Calls internal methods via reflection (Editor assembly != Runtime assembly)
    static void Invoke(object obj, string method, params object[] args)
    {
        var mi = obj.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        mi?.Invoke(obj, args.Length > 0 ? args : null);
    }

    void Bold(string t) => EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
    void Space(int px = 4) => EditorGUILayout.Space(px);
    void BoolRow(string label, bool val)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(180));
        Tint(val ? "✓  true" : "✗  false", val ? ColPass : ColFail);
        EditorGUILayout.EndHorizontal();
    }

    void Tint(string text, Color col)
    {
        var old = GUI.color; GUI.color = col;
        GUILayout.Label(text, EditorStyles.miniLabel);
        GUI.color = old;
    }
}
#endif

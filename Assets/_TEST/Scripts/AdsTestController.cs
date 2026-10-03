using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Giả lập vòng chơi đơn giản để test các luồng ads: level, revive, x2 coin, MREC, banner, AOA, resume.
public class AdsTestController : MonoBehaviour
{
    public Transform Player;

    private int coin;
    private int level = 1;
    private Text hudText;
    private Text logText;
    private readonly Queue<string> logs = new Queue<string>();
    private Font font;

    private AdsManager Ads => AdsManager.Instance;

    private void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        RefreshHud();
    }

    private void Update()
    {
        if (Player != null) Player.Rotate(0f, 60f * Time.deltaTime, 0f);
    }

    #region Game Actions
    private void CompleteLevel()
    {
        AddLog($"Level {level} complete -> Interstitial");
        Ads.InterstitialModule.ShowInterstitial("level_complete", () =>
        {
            level++;
            coin += 10;
            AddLog($"Inter callback -> Level {level}");
            RefreshHud();
        });
    }

    private void ClosePopup()
    {
        AddLog("Close popup -> Interstitial (popupClose)");
        Ads.InterstitialModule.ShowInterstitial("popup_close", () => AddLog("Inter (popupClose) callback"), true);
    }

    private void Revive()
    {
        AddLog("Revive -> Rewarded");
        Ads.RewardedModule.ShowRewarded("revive",
            () => { AddLog("<color=lime>Revive SUCCESS</color>"); },
            () => { AddLog("<color=red>Revive FAIL (not ready / closed / error)</color>"); });
    }

    private void DoubleCoin()
    {
        AddLog("x2 Coin -> Rewarded");
        Ads.RewardedModule.ShowRewarded("double_coin",
            () => { coin *= 2; RefreshHud(); AddLog("<color=lime>x2 Coin SUCCESS</color>"); },
            () => AddLog("<color=red>x2 Coin FAIL</color>"));
    }

    private void SimulateResume()
    {
        AddLog("Simulate background -> resume");
        Ads.SendMessage("OnApplicationPause", true);
        Ads.SendMessage("OnApplicationPause", false);
    }

#if USE_IAP
    public void BuyIAP(string productId)
    {
        var iap = IAPManager.Instance;
        AddLog($"IAP buy {productId} | connected={iap.IsConnected} products={iap.IsProductsReady}");
        iap.NotifyPurchaseFlow(true);
        iap.PurchaseModule.Purchase(productId, (id, ok) =>
        {
            iap.NotifyPurchaseFlow(false);
            if (ok && id == "com.yourgame.money_small") { coin += 100; RefreshHud(); }
            AddLog(ok ? $"<color=lime>IAP {id} SUCCESS</color> removeAds={Ads.IsRemoveAdsPurchased}" : $"<color=red>IAP {id} FAIL</color>");
        });
    }
#endif
    #endregion

    #region UI
    private void BuildUI()
    {
        var canvasGo = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        hudText = CreateText(canvasGo.transform, "HUD", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(0, 100), 44, TextAnchor.MiddleCenter);

        var panel = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.05f, 0.3f);
        prt.anchorMax = new Vector2(0.95f, 0.88f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childForceExpandHeight = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        AddButton(panel.transform, "Complete Level (Inter)", CompleteLevel);
        AddButton(panel.transform, "Close Popup (Inter popupClose)", ClosePopup);
        AddButton(panel.transform, "Revive (Rewarded)", Revive);
        AddButton(panel.transform, "x2 Coin (Rewarded)", DoubleCoin);
        AddButton(panel.transform, "Show Banner", () => { Ads.BannerModule.ShowBanner(); AddLog("Show Banner"); });
        AddButton(panel.transform, "Hide Banner", () => { Ads.BannerModule.HideBanner(); AddLog("Hide Banner"); });
        AddButton(panel.transform, "Show MREC", () => { Ads.MRECModule.ShowMREC(); AddLog("Show MREC"); });
        AddButton(panel.transform, "Hide MREC", () => { Ads.MRECModule.HideMREC(); AddLog("Hide MREC"); });
        AddButton(panel.transform, "AOA (loading)", () => { Ads.AppOpenModule.ShowAppOpen(true); AddLog("AOA loading"); });
        AddButton(panel.transform, "AOA (resume)", () => { Ads.AppOpenModule.ShowAppOpen(false); AddLog("AOA resume"); });
        AddButton(panel.transform, "Simulate Background/Resume", SimulateResume);
        AddButton(panel.transform, "Open MAX Mediation Debugger", () => MaxSdk.ShowMediationDebugger());
#if USE_IAP
        AddButton(panel.transform, "IAP Buy Money (Consumable)", () => BuyIAP("com.yourgame.money_small"));
        AddButton(panel.transform, "IAP Buy Remove Ads", () => BuyIAP("com.yourgame.remove_ads"));
        AddButton(panel.transform, "IAP Restore", () => IAPManager.Instance.RestorePurchases(ok => AddLog($"Restore -> {ok}")));
#endif

        logText = CreateText(canvasGo.transform, "Log", new Vector2(0, 0), new Vector2(1, 0.28f), Vector2.zero, Vector2.zero, 28, TextAnchor.LowerLeft);
        logText.rectTransform.offsetMin = new Vector2(30, 180);
        logText.rectTransform.offsetMax = new Vector2(-30, -10);
    }

    private Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.supportRichText = true;
        return t;
    }

    private void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 0.9f);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        var t = CreateText(go.transform, "Text", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 25, TextAnchor.MiddleCenter);
        t.text = label;
    }

    private void RefreshHud()
    {
        hudText.text = $"Level {level}    Coin {coin}";
    }

    private void AddLog(string msg)
    {
        logs.Enqueue($"[{Time.time:0.0}] {msg}");
        while (logs.Count > 8) logs.Dequeue();
        logText.text = string.Join("\n", logs);
        LogMessage.Log(msg, "AdsTest");
    }
    #endregion
}

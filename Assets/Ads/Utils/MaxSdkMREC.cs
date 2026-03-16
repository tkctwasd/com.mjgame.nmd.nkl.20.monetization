using UnityEngine;
using UnityEngine.UI;

public class MaxSdkMREC
{
    public static void ShowMRec(string id, MaxSdkBase.AdViewPosition pos = MaxSdkBase.AdViewPosition.TopLeft)
    {
#if UNITY_EDITOR
        if (GameObject.Find($"Mrec(Clone)")) return;

        GameObject root = new GameObject($"Mrec(Clone)");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        Object.DontDestroyOnLoad(root);

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        root.AddComponent<GraphicRaycaster>();

        GameObject mrecObj = new GameObject("Container");
        mrecObj.transform.SetParent(root.transform, false);

        RectTransform rt = mrecObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800, 500);

        SetAnchorByPosition(rt, pos);

        Image img = mrecObj.AddComponent<Image>();

        GameObject label = new GameObject("Text");
        label.transform.SetParent(mrecObj.transform, false);

        Text t = label.AddComponent<Text>();

        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        t.text = $"<color=#323232><size=26>MAX Mrec Ad:\n{id}</size>\n<size=18>Actual mrec size may vary on mobile devices</size></color>";
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform textRt = t.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
#endif
    }

    public static void HideMREC()
    {
#if UNITY_EDITOR
        GameObject root = GameObject.Find($"Mrec(Clone)");
        if (root != null)
        {
            if (Application.isPlaying)
                Object.Destroy(root);
            else
                Object.DestroyImmediate(root);
        }
#endif
    }

    private static void SetAnchorByPosition(RectTransform rt, MaxSdkBase.AdViewPosition pos)
    {
        Vector2 anchor = new Vector2(0.5f, 0.5f);
        Vector2 pivot = new Vector2(0.5f, 0.5f);

        switch (pos)
        {
            case MaxSdkBase.AdViewPosition.TopLeft:
                anchor = new Vector2(0, 1); pivot = new Vector2(0, 1); break;
            case MaxSdkBase.AdViewPosition.TopCenter:
                anchor = new Vector2(0.5f, 1); pivot = new Vector2(0.5f, 1); break;
            case MaxSdkBase.AdViewPosition.TopRight:
                anchor = new Vector2(1, 1); pivot = new Vector2(1, 1); break;
            case MaxSdkBase.AdViewPosition.Centered:
                anchor = new Vector2(0.5f, 0.5f); pivot = new Vector2(0.5f, 0.5f); break;
            case MaxSdkBase.AdViewPosition.BottomLeft:
                anchor = new Vector2(0, 0); pivot = new Vector2(0, 0); break;
            case MaxSdkBase.AdViewPosition.BottomCenter:
                anchor = new Vector2(0.5f, 0); pivot = new Vector2(0.5f, 0); break;
            case MaxSdkBase.AdViewPosition.BottomRight:
                anchor = new Vector2(1, 0); pivot = new Vector2(1, 0); break;
            case MaxSdkBase.AdViewPosition.CenterLeft:
                anchor = new Vector2(0, 0.5f); pivot = new Vector2(0, 0.5f); break;
            case MaxSdkBase.AdViewPosition.CenterRight:
                anchor = new Vector2(1, 0.5f); pivot = new Vector2(1, 0.5f); break;
        }

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
    }
}
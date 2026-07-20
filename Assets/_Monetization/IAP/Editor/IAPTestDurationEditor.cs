using UnityEditor;
using UnityEngine;

/// <summary>
/// Đổi nhanh thời hạn subscription khi test, không phải sửa code hay đổi SubscriptionDays.
/// Tools > IAP > Subscription Test Duration
/// </summary>
public static class IAPTestDurationEditor
{
    const string Menu = "Tools/IAP/Test Packs/Time/";

    [MenuItem(Menu + "1 phút")]
    static void OneMinute() => Set(1f);

    [MenuItem(Menu + "Off")]
    static void Off() => Set(0f);

    [MenuItem(Menu + "1 phút", true)]
    static bool OneMinuteCheck() => Check(1f);

    [MenuItem(Menu + "Tắt (dùng SubscriptionDays thật)", true)]
    static bool OffCheck() => Check(0f);

    static void Set(float minutes)
    {
        MonetizationEntitlements.TestSubscriptionMinutes = minutes;

        string msg = minutes > 0f
            ? $"Subscription sẽ hết hạn sau {minutes} phút kể từ lúc mua.\n\n" +
              "Chỉ rút ngắn phía client — gói trên store vẫn là weekly.\n" +
              "Khi bật, nút mua bỏ qua trạng thái store và chỉ tin dữ liệu local."
            : "Đã trả về số ngày thật khai trong IAPProducts.";

        EditorUtility.DisplayDialog("Subscription Test Duration", msg, "OK");
    }

    // Tick vào mục đang chọn.
    static bool Check(float minutes)
    {
        Menu_SetChecked(minutes);
        return true;
    }

    static void Menu_SetChecked(float minutes)
    {
        float current = MonetizationEntitlements.TestSubscriptionMinutes;
        string item = minutes > 0f ? $"{minutes:0} phút" : "Tắt (dùng SubscriptionDays thật)";
        UnityEditor.Menu.SetChecked(Menu + item, Mathf.Approximately(current, minutes));
    }
}

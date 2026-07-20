using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Điền sẵn bộ pack test vào IAPProducts để không phải gõ tay mỗi lần dựng game mới.
/// Tools > IAP > Fill Test Packs
/// </summary>
public static class IAPTestPacksEditor
{
    const string Prefix = "com.yourgame.test";

    [MenuItem("Tools/IAP/Fill Test Packs")]
    static void FillTestPacks()
    {
        var asset = LoadProductsAsset();
        if (asset == null)
        {
            EditorUtility.DisplayDialog("IAP Test Packs",
                "Không tìm thấy asset IAPProducts nào trong project.\n" +
                "Tạo trước qua: Create > IAP > IAP Products", "OK");
            return;
        }

        bool replace = EditorUtility.DisplayDialog("IAP Test Packs",
            $"Ghi {BuildTestPacks().Count} pack test vào '{asset.name}'?\n\n" +
            "Chọn Ghi đè để xoá sạch danh sách hiện tại.\n" +
            "Chọn Thêm vào để giữ pack cũ và chỉ thêm pack test còn thiếu.",
            "Ghi đè", "Thêm vào");

        Undo.RecordObject(asset, "Fill IAP Test Packs");

        if (replace) asset.products.Clear();

        int added = 0;
        foreach (var pack in BuildTestPacks())
        {
            if (asset.products.Exists(p => p.ProductId == pack.ProductId)) continue;
            asset.products.Add(pack);
            added++;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>[IAPTestPacks]</color> Đã thêm {added} pack vào '{asset.name}'. " +
                  $"Nhớ tạo đúng các SKU này trên Play Console / App Store Connect.");
        Selection.activeObject = asset;
    }

    static IAPProducts LoadProductsAsset()
    {
        var guids = AssetDatabase.FindAssets("t:IAPProducts");
        if (guids.Length == 0) return null;

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<IAPProducts>(path);
    }

    /// <summary>
    /// Ma trận pack test. Dấu "+" trong tên = pack gộp nhiều loại thưởng.
    /// </summary>
    static List<IAPProduct> BuildTestPacks()
    {
        return new List<IAPProduct>
        {
            // ── Consumable: mua lại được nhiều lần ────────────────────────────
            new($"{Prefix}.con_gem10", ProductType.Consumable)
            {
                Title = "[TEST] 10 Gem",
                Description = "Consumable — mua nhiều lần, mỗi lần +10 gem.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Gem, Gem = 10 }
                }
            },
            new($"{Prefix}.con_coin10", ProductType.Consumable)
            {
                Title = "[TEST] 10 Coin",
                Description = "Consumable — kiểm tra loại tiền tệ thứ hai.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Money, Money = 10 }
                }
            },

            // ── NonConsumable: mua một lần, restore được ──────────────────────
            new($"{Prefix}.non_gem10", ProductType.NonConsumable)
            {
                Title = "[TEST] 10 Gem (một lần)",
                Description = "NonConsumable cấp tiền tệ — CA BẪY: restore không được cấp lại gem.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Gem, Gem = 10 }
                }
            },
            new($"{Prefix}.non_removeads", ProductType.NonConsumable)
            {
                Title = "[TEST] Remove Ads",
                Description = "NonConsumable — tắt banner/inter/AOA/MREC vĩnh viễn, rewarded vẫn còn.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.RemoveAds }
                }
            },
            new($"{Prefix}.non_gem100_removeads", ProductType.NonConsumable)
            {
                Title = "[TEST] 100 Gem + Remove Ads",
                Description = "Pack gộp — mẫu để nhân bản cho các pack combo sau này.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Gem, Gem = 100 },
                    new() { RewardType = IAPRewardType.RemoveAds }
                }
            },

            // ── Subscription: 2 ngày, hết hạn phải thu hồi quyền lợi ──────────
            new($"{Prefix}.sub_gem100", ProductType.Subscription)
            {
                Title = "[TEST] SUB 2 ngày — 100 Gem",
                Description = "Subscription cấp gem. Kiểm tra gia hạn có cấp lại không.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Gem, Gem = 100 },
                    new() { RewardType = IAPRewardType.Subscription, SubscriptionDays = 2 }
                }
            },
            new($"{Prefix}.sub_shield", ProductType.Subscription)
            {
                Title = "[TEST] SUB 2 ngày — Shield",
                Description = "Subscription cấp shield. Hết hạn phải mất shield.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.Shield, ShieldCount = 1 },
                    new() { RewardType = IAPRewardType.Subscription, SubscriptionDays = 2 }
                }
            },
            new($"{Prefix}.sub_removeads", ProductType.Subscription)
            {
                Title = "[TEST] SUB 2 ngày — Remove Ads",
                Description = "CA BẪY: remove-ads có thời hạn. Hết hạn ads PHẢI hiện lại.",
                Rewards = new List<IAPReward>
                {
                    new() { RewardType = IAPRewardType.RemoveAds },
                    new() { RewardType = IAPRewardType.Subscription, SubscriptionDays = 2 }
                }
            },
        };
    }
}

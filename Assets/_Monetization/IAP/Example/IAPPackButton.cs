using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Một dòng pack trong IAPPack. Hiển thị title / price / phần thưởng,
/// và khoá nút lại nếu là loại chỉ mua một lần mà người chơi đã sở hữu.
/// </summary>
public class IAPPackButton : IAPButtonUI
{
    [Header("Pack")]
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI ownedText;

    private IAPProduct data;

    protected override void RefreshUI()
    {
        base.RefreshUI();

        data = FindData();
        if (data == null) return;

        // Product chưa fetch được (offline / sai id trên store) → base đã để nút ở trạng thái loading.
        // Vẫn hiển thị title từ ScriptableObject để tester biết pack nào đang thiếu.
        if (titleText != null && string.IsNullOrEmpty(titleText.text))
            titleText.text = data.Title;

        if (rewardText != null)
            rewardText.text = DescribeRewards(data.Rewards);

        RefreshOwnedState();
    }

    private IAPProduct FindData()
    {
        var products = IAPManager.Instance != null ? IAPManager.Instance.IAPProducts : null;
        if (products == null) return null;

        return products.products.Find(p => p.ProductId == ProductId);
    }

    private void RefreshOwnedState()
    {
        // Consumable mua lại được nhiều lần nên không bao giờ khoá.
        bool onceOnly = data.ProductType == ProductType.NonConsumable
                        || data.ProductType == ProductType.Subscription;
        if (!onceOnly)
        {
            if (ownedText != null) ownedText.gameObject.SetActive(false);
            return;
        }

        // Local trước: đúng ngay lúc mở app và cả khi offline.
        // Store chỉ dùng để bù cho máy mới cài lại, lúc local chưa có gì.
        bool owned = MonetizationEntitlements.IsOwned(ProductId)
                     || (!MonetizationEntitlements.TrustLocalOnly
                         && IAPManager.Instance != null
                         && IAPManager.Instance.PurchaseModule != null
                         && IAPManager.Instance.PurchaseModule.IsPurchased(ProductId));

        if (ownedText != null)
        {
            ownedText.gameObject.SetActive(owned);
            ownedText.text = BuildOwnedLabel();
        }

        if (owned && button != null)
            button.interactable = false;
    }

    private string BuildOwnedLabel()
    {
        if (data.ProductType != ProductType.Subscription)
            return "<color=red>Owned</color>";

        var expiry = MonetizationEntitlements.GetExpiry(ProductId);
        return expiry == null
            ? "<color=#0065FF>Subscribed</color>"
            : $"<color=#0065FF>Subscribed</color> — {expiry.Value.ToLocalTime():dd/MM HH:mm}";
    }

    private static string DescribeRewards(List<IAPReward> rewards)
    {
        if (rewards == null || rewards.Count == 0) return "-";

        var sb = new StringBuilder();
        foreach (var reward in rewards)
        {
            if (sb.Length > 0) sb.Append("  +  ");

            switch (reward.RewardType)
            {
                case IAPRewardType.Money:
                    sb.Append($"{reward.Money:N0} Money");
                    break;
                case IAPRewardType.Gem:
                    sb.Append($"{reward.Gem:N0} Gem");
                    break;
                case IAPRewardType.Brainrot:
                    sb.Append(string.IsNullOrEmpty(reward.BrainrotName) ? "Brainrot" : reward.BrainrotName);
                    break;
                case IAPRewardType.RemoveAds:
                    sb.Append("Remove Ads");
                    break;
                case IAPRewardType.Subscription:
                    sb.Append($"VIP {reward.SubscriptionDays} ngày");
                    break;
                default:
                    sb.Append(reward.RewardType.ToString());
                    break;
            }
        }

        return sb.ToString();
    }

    protected override void OnPurchaseSuccess(string productId, ConfirmedOrder order)
    {
        base.OnPurchaseSuccess(productId, order);
        Debug.Log("IAPPackButton".IAP($"<color=green>Mua thành công: {productId}</color>"));
        RefreshUI();
    }

    protected override void OnPurchaseFailed(string productId)
    {
        base.OnPurchaseFailed(productId);
        Debug.LogWarning("IAPPackButton".IAP($"<color=red>Mua thất bại: {productId}</color>"));
    }

    public override void OnRestoreComplete(bool success)
    {
        base.OnRestoreComplete(success);
        RefreshUI();
    }
}

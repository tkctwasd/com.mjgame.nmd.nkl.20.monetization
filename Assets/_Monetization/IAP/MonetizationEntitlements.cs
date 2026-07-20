using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Nơi duy nhất cấp thưởng IAP và giữ quyền lợi của người chơi.
///
/// Nghe cả hai đường:
///  - OnProductDelivered  → vừa mua xong
///  - OnPurchasesReady    → mở app / restore / cài lại máy
///
/// Chống cấp trùng bằng TransactionID nên restore bao nhiêu lần cũng chỉ cộng một lần.
/// Tự sinh khi game chạy, không cần đặt vào scene hay Resources.
/// </summary>
public class MonetizationEntitlements : MonoBehaviour
{
    public static MonetizationEntitlements Instance { get; private set; }

    /// <summary>Bắn khi số dư hoặc quyền lợi đổi, để UI vẽ lại.</summary>
    public static event Action OnChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;

        var go = new GameObject(nameof(MonetizationEntitlements));
        go.AddComponent<MonetizationEntitlements>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() => Subscribe();

    void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this) Instance = null;
    }

    #region Subscribe

    bool subscribed;

    void Subscribe()
    {
        if (subscribed || IAPManager.Instance == null) return;

        IAPManager.Instance.OnProductDelivered += OnProductDelivered;
        IAPManager.Instance.OnPurchasesReady += OnPurchasesReady;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed || IAPManager.Instance == null) return;

        IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
        IAPManager.Instance.OnPurchasesReady -= OnPurchasesReady;
        subscribed = false;
    }

    // IAPManager.Start() có thể chạy sau Start() của class này nên thử lại cho tới khi bắt được.
    void Update()
    {
        if (!subscribed) Subscribe();
    }

    #endregion

    #region Grant

    void OnProductDelivered(string productId, ConfirmedOrder order)
    {
        Grant(productId, order != null ? order.Info.TransactionID : null);
    }

    void OnPurchasesReady(Orders orders)
    {
        if (orders == null) return;

        foreach (var order in orders.ConfirmedOrders)
        {
            if (order == null) continue;

            foreach (var item in order.CartOrdered.Items())
                Grant(item.Product.definition.id, order.Info.TransactionID);
        }
    }

    /// <summary>
    /// Cấp thưởng của một product. Bỏ qua nếu transaction này đã cấp rồi.
    /// Consumable mua lại luôn có TransactionID mới nên vẫn cộng dồn bình thường.
    /// </summary>
    void Grant(string productId, string transactionId)
    {
        var data = Data;
        if (data == null) return;

        if (data.WasGranted(transactionId))
            return;

        var rewards = Products != null ? Products.GetRewardsByProductId(productId) : null;
        if (rewards == null || rewards.Count == 0)
        {
            Debug.LogWarning("Entitlements".IAP($"Không có reward khai báo cho '{productId}'."));
            return;
        }

        // Pack subscription: tính hạn trước để RemoveAds trong cùng pack dùng chung mốc.
        DateTime? expiry = FindExpiry(rewards);

        foreach (var reward in rewards)
            Apply(data, reward, expiry);

        // Consumable mua lại được nên không ghi sở hữu, chỉ ghi loại mua-một-lần.
        if (IsOnceOnly(productId))
            data.SetOwned(productId, expiry);

        data.MarkGranted(transactionId);
        Save();

        Debug.Log("Entitlements".IAP(
            $"<color=green>Cấp thưởng '{productId}'</color> | tx={transactionId} | " +
            $"Money={data.Money} Gem={data.Gem} Shield={data.Shield} RemoveAds={data.HasRemoveAds}"));

        OnChanged?.Invoke();
    }

    /// <summary>NonConsumable / Subscription = mua một lần, phải khoá nút sau khi sở hữu.</summary>
    static bool IsOnceOnly(string productId)
    {
        var products = Products;
        if (products == null) return false;

        var entry = products.products.Find(p => p.ProductId == productId);
        if (entry == null) return false;

        return entry.ProductType == ProductType.NonConsumable
               || entry.ProductType == ProductType.Subscription;
    }

    static DateTime? FindExpiry(List<IAPReward> rewards)
    {
        foreach (var reward in rewards)
        {
            if (reward.RewardType == IAPRewardType.Subscription && reward.SubscriptionDays > 0)
                return DateTime.UtcNow.AddDays(reward.SubscriptionDays);
        }

        return null; // vĩnh viễn
    }

    static void Apply(DataMonetization data, IAPReward reward, DateTime? expiry)
    {
        switch (reward.RewardType)
        {
            case IAPRewardType.Money:
                data.Money += reward.Money;
                break;

            case IAPRewardType.Gem:
                data.Gem += reward.Gem;
                break;

            case IAPRewardType.Shield:
                data.Shield += reward.ShieldCount;
                break;

            case IAPRewardType.RemoveAds:
                data.GrantRemoveAds(expiry);
                HideAdsNow();
                break;

            case IAPRewardType.Subscription:
                // Thời hạn đã xử lý ở FindExpiry.
                break;

            default:
                Debug.LogWarning("Entitlements".IAP($"Chưa xử lý reward '{reward.RewardType}'."));
                break;
        }
    }

    static void HideAdsNow()
    {
        if (AdsManager.Instance == null) return;

        AdsManager.Instance.BannerModule.HideBanner();
        AdsManager.Instance.MRECModule.HideMREC();
    }

    #endregion

    #region Query

    static DataMonetization Data =>
        DataSave.Instance != null ? DataSave.Instance.DataMonetization : null;

    static IAPProducts Products =>
        IAPManager.Instance != null ? IAPManager.Instance.IAPProducts : null;

    /// <summary>
    /// Người chơi có đang được remove-ads không. Đọc từ local (có xét hạn) nên hoạt động cả khi offline.
    /// </summary>
    public static bool HasRemoveAds
    {
        get
        {
            var data = Data;
            return data != null && data.HasRemoveAds;
        }
    }

    /// <summary>
    /// Product loại mua-một-lần này còn hiệu lực không. Đọc local nên đúng ngay lúc mở app
    /// và cả khi offline, không phải chờ store trả purchases.
    /// </summary>
    public static bool IsOwned(string productId)
    {
        var data = Data;
        return data != null && data.IsOwned(productId);
    }

    /// <summary>Hạn của một product. null = vĩnh viễn hoặc chưa sở hữu.</summary>
    public static DateTime? GetExpiry(string productId)
    {
        var data = Data;
        return data != null ? data.GetExpiry(productId) : null;
    }

    public static double Money => Data != null ? Data.Money : 0;
    public static int Gem => Data != null ? Data.Gem : 0;
    public static int Shield => Data != null ? Data.Shield : 0;

    /// <summary>Hạn remove-ads để hiển thị. null = vĩnh viễn hoặc chưa mua.</summary>
    public static DateTime? RemoveAdsExpiry
    {
        get
        {
            var data = Data;
            if (data == null) return null;
            if (data.RemoveAdsExpiryTicks is 0 or long.MaxValue) return null;

            return new DateTime(data.RemoveAdsExpiryTicks, DateTimeKind.Utc);
        }
    }

    static void Save()
    {
        if (DataSave.Instance != null) DataSave.Instance.SaveGame();
    }

    #endregion

    #region Debug

    /// <summary>Xoá sạch quyền lợi + số dư. Dùng khi test lại từ đầu.</summary>
    public static void ResetForTesting()
    {
        if (DataSave.Instance == null) return;

        DataSave.Instance.NewGame();
        OnChanged?.Invoke();
        Debug.Log("Entitlements".IAP("<color=orange>Đã reset toàn bộ entitlement.</color>"));
    }

    #endregion
}

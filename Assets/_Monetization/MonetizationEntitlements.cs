using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class MonetizationEntitlements : MonoBehaviour
{
    public static MonetizationEntitlements Instance { get; private set; }

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

        DateTime? expiry = FindExpiry(rewards);

        foreach (var reward in rewards)
            Apply(data, reward, expiry);

        if (IsOnceOnly(productId))
            data.SetOwned(productId, expiry);

        data.MarkGranted(transactionId);
        Save();

        Debug.Log("Entitlements".IAP(
            $"<color=green>Cấp thưởng '{productId}'</color> | tx={transactionId} | " +
            $"Money={Money} Gem={Gem} Shield={Shield} RemoveAds={data.HasRemoveAds}"));

        OnChanged?.Invoke();
    }

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
            if (reward.RewardType != IAPRewardType.Subscription || reward.SubscriptionDays <= 0)
                continue;

            if (TestSubscriptionMinutes > 0f)
                return DateTime.UtcNow.AddMinutes(TestSubscriptionMinutes);

            return DateTime.UtcNow.AddDays(reward.SubscriptionDays);
        }

        return null;
    }

    static void Apply(DataMonetization data, IAPReward reward, DateTime? expiry)
    {
        var wallet = Wallet;

        switch (reward.RewardType)
        {
            case IAPRewardType.Money:
                if (wallet != null) wallet.AddMoney(reward.Money);
                break;

            case IAPRewardType.Gem:
                if (wallet != null) wallet.AddGem(reward.Gem);
                break;

            case IAPRewardType.Shield:
                if (wallet != null) wallet.AddShield(reward.ShieldCount);
                break;

            case IAPRewardType.RemoveAds:
                data.GrantRemoveAds(expiry);
                HideAdsNow();
                break;

            case IAPRewardType.Subscription:
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

    static DataMonetization Data => DataSave.Instance != null ? DataSave.Instance.DataMonetization : null;

    static DataGamePlay Wallet => DataSave.Instance != null ? DataSave.Instance.DataGamePlay : null;

    static IAPProducts Products => IAPManager.Instance != null ? IAPManager.Instance.IAPProducts : null;
    public static bool HasRemoveAds
    {
        get
        {
            var data = Data;
            return data != null && data.HasRemoveAds;
        }
    }

    public static bool IsOwned(string productId)
    {
        var data = Data;
        return data != null && data.IsOwned(productId);
    }

    public static DateTime? GetExpiry(string productId)
    {
        var data = Data;
        return data != null ? data.GetExpiry(productId) : null;
    }

    public static double Money => Wallet != null ? Wallet.Money : 0;
    public static int Gem => Wallet != null ? Wallet.Gem : 0;
    public static int Shield => Wallet != null ? Wallet.Shield : 0;

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

    const string KeyTestSubMinutes = "test_sub_minutes";

    public static float TestSubscriptionMinutes
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return PlayerPrefs.GetFloat(KeyTestSubMinutes, 0f);
#else
            return 0f; 
#endif
        }
        set
        {
            PlayerPrefs.SetFloat(KeyTestSubMinutes, Mathf.Max(0f, value));
            PlayerPrefs.Save();
            Debug.Log("Entitlements".IAP(value > 0f
                ? $"<color=orange>TEST: subscription hết hạn sau {value} phút.</color>"
                : "<color=orange>TEST: subscription dùng lại số ngày thật.</color>"));
        }
    }

    public static bool TrustLocalOnly => TestSubscriptionMinutes > 0f;

    public static void ResetForTesting()
    {
        if (DataSave.Instance == null) return;

        DataSave.Instance.NewGame();
        OnChanged?.Invoke();
        Debug.Log("Entitlements".IAP("<color=orange>Đã reset toàn bộ entitlement.</color>"));
    }

    #endregion
}

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

/// <summary>
/// Panel test IAP: liệt kê toàn bộ pack khai báo trong IAPProducts để mua thử trên máy thật.
/// Danh sách sinh động từ ScriptableObject nên thêm pack mới không cần sửa scene.
/// </summary>
public class IAPPack : UIPanel
{
    [Header("Spawn")]
    [SerializeField] private IAPPackButton packButtonPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Show")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gemText;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private TextMeshProUGUI removeAdsText;

    [Header("Actions")]
    [SerializeField] private Button restoreButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    private readonly List<IAPPackButton> spawned = new();
    private bool isBuilt;

    protected override void Awake()
    {
        base.Awake();

        if (restoreButton != null) restoreButton.onClick.AddListener(OnRestoreClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
    }

    public override void Show()
    {
        base.Show();

        Build();
        Subscribe();

        // Build() bỏ qua từ lần mở thứ hai, nên phải vẽ lại tay để không giữ trạng thái cũ.
        RefreshAll();
    }

    public override void Hide()
    {
        Unsubscribe();
        base.Hide();
    }

    private void OnDestroy() => Unsubscribe();

    #region Build

    private void Build()
    {
        if (isBuilt) return;

        if (packButtonPrefab == null || contentParent == null)
        {
            Debug.LogError("IAPPack".IAP("Chưa gán packButtonPrefab hoặc contentParent."));
            return;
        }

        var products = IAPManager.Instance != null ? IAPManager.Instance.IAPProducts : null;
        if (products == null)
        {
            Debug.LogError("IAPPack".IAP("IAPManager hoặc IAPProducts null. Kiểm tra prefab IAPManager trong Resources."));
            return;
        }

        foreach (var product in products.products)
        {
            if (string.IsNullOrWhiteSpace(product.ProductId)) continue;

            var item = Instantiate(packButtonPrefab, contentParent);
            item.name = $"Pack_{product.ProductId}";
            // Setup phải chạy ngay sau Instantiate: Awake/OnEnable đã xong, Start thì chưa.
            item.Setup(product.ProductId);
            spawned.Add(item);
        }

        isBuilt = true;
        Debug.Log("IAPPack".IAP($"Đã spawn {spawned.Count} pack."));
    }

    private void RefreshAll()
    {
        foreach (var item in spawned)
        {
            if (item != null) item.Refresh();
        }

        RefreshStatus();
        RefreshBalance();
    }

    #endregion

    #region Events

    private void Subscribe()
    {
        MonetizationEntitlements.OnChanged -= OnEntitlementsChanged;
        MonetizationEntitlements.OnChanged += OnEntitlementsChanged;

        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnProductsReady += OnProductsReady;
        IAPManager.Instance.OnProductsFetchFailed += OnProductsFetchFailed;
        IAPManager.Instance.OnPurchasesReady += OnPurchasesReady;
        IAPManager.Instance.OnProductDelivered += OnProductDelivered;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        IAPManager.Instance.OnRestoreComplete += OnRestoreComplete;
    }

    private void Unsubscribe()
    {
        MonetizationEntitlements.OnChanged -= OnEntitlementsChanged;

        if (IAPManager.Instance == null) return;

        IAPManager.Instance.OnProductsReady -= OnProductsReady;
        IAPManager.Instance.OnProductsFetchFailed -= OnProductsFetchFailed;
        IAPManager.Instance.OnPurchasesReady -= OnPurchasesReady;
        IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnRestoreComplete -= OnRestoreComplete;
    }

    private void OnProductsReady(List<Product> products) => RefreshStatus();
    private void OnProductsFetchFailed() => RefreshStatus();
    private void OnPurchasesReady(Orders orders) => RefreshAll();
    private void OnProductDelivered(string productId, ConfirmedOrder order) => RefreshAll();
    private void OnPurchaseFailed(FailedOrder failed) => RefreshStatus();

    private void OnRestoreComplete(bool success)
    {
        if (restoreButton != null) restoreButton.interactable = true;
        Debug.Log("IAPPack".IAP($"Restore {(success ? "thành công" : "thất bại")}."));
        RefreshAll();
    }

    #endregion

    #region Actions

    private void OnRestoreClicked()
    {
        if (IAPManager.Instance == null) return;

        if (restoreButton != null) restoreButton.interactable = false;
        IAPManager.Instance.RestorePurchases();
    }

    private void OnResetClicked()
    {
        MonetizationEntitlements.ResetForTesting();
        RefreshAll();
    }

    private void OnEntitlementsChanged() => RefreshAll();

    #endregion

    #region Balance

    /// <summary>
    /// Vẽ số dư thật đọc từ DataSave — dùng để xác nhận reward có cộng vào hay không.
    /// </summary>
    private void RefreshBalance()
    {
        if (coinText != null) coinText.text = $"Coin: {IAPExampleRewardHandler.Money:N0}";
        if (gemText != null) gemText.text = $"Gem: {IAPExampleRewardHandler.Gem:N0}";
        if (shieldText != null) shieldText.text = $"Shield: {IAPExampleRewardHandler.Shield:N0}";

        if (removeAdsText == null) return;

        if (!MonetizationEntitlements.HasRemoveAds)
        {
            removeAdsText.text = "RemoveAds: <color=#C0C020>OFF</color>";
            return;
        }

        var expiry = MonetizationEntitlements.RemoveAdsExpiry;
        removeAdsText.text = expiry == null
            ? "RemoveAds: <color=#20C060>ON (vĩnh viễn)</color>"
            : $"RemoveAds: <color=#20C060>ON</color> — hết hạn {expiry.Value.ToLocalTime():dd/MM HH:mm}";
    }

    #endregion

    #region Status

    private void RefreshStatus()
    {
        if (statusText == null) return;

        var manager = IAPManager.Instance;
        if (manager == null)
        {
            statusText.text = "<color=red>IAPManager chưa khởi tạo</color>";
            return;
        }

        var sb = new StringBuilder();
        sb.Append(Flag("Store", manager.IsConnected));
        sb.Append("   ");
        sb.Append(Flag("Products", manager.IsProductsReady));
        sb.Append("   ");
        sb.Append(Flag("Purchases", manager.IsPurchasesReady));

        statusText.text = sb.ToString();
    }

    private static string Flag(string label, bool ok)
    {
        return ok
            ? $"<color=#20C060>{label} OK</color>"
            : $"<color=#C0C020>{label} ...</color>";
    }

    #endregion
}

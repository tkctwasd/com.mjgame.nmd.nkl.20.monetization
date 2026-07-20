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

    [Header("Actions")]
    [SerializeField] private Button restoreButton;
    [SerializeField] private Button closeButton;

    private readonly List<IAPPackButton> spawned = new();
    private bool isBuilt;

    protected override void Awake()
    {
        base.Awake();

        if (restoreButton != null) restoreButton.onClick.AddListener(OnRestoreClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public override void Show()
    {
        base.Show();

        Build();
        Subscribe();
        RefreshStatus();
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
    }

    #endregion

    #region Events

    private void Subscribe()
    {
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

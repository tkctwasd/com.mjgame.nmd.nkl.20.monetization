using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class IAPButtonUI : MonoBehaviour
{
    [Header("Product")]
    [ColorLabel] public string ProductId;
    [Space]

    [SerializeField] protected TextMeshProUGUI titleText;
    [SerializeField] protected TextMeshProUGUI descriptionText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] private GameObject loadingOverlay;

    protected Button button;

    protected virtual void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    protected virtual IEnumerator Start()
    {
        // Đợi products sẵn sàng rồi mới điền UI
        yield return new WaitUntil(() =>
            IAPManager.Instance != null && IAPManager.Instance.IsProductsReady);
        RefreshUI();
    }

    protected virtual void OnEnable()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnProductsReady += OnProductsReady;
            IAPManager.Instance.OnProductDelivered += OnProductDelivered;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        }
    }

    protected virtual void OnDisable()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnProductsReady -= OnProductsReady;
            IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        }
    }


    void OnProductsReady(System.Collections.Generic.List<UnityEngine.Purchasing.Product> products)
        => RefreshUI();

    void OnProductDelivered(string productId, UnityEngine.Purchasing.ConfirmedOrder order)
    {
        if (productId != ProductId) return;
        SetLoading(false);
        OnPurchaseSuccess(productId, order);
    }

    void OnPurchaseFailed(UnityEngine.Purchasing.FailedOrder failed)
    {
        bool mine = false;
        foreach (var item in failed.CartOrdered.Items())
            if (item.Product.definition.id == ProductId) { mine = true; break; }
        if (!mine) return;

        SetLoading(false);
        OnPurchaseFailed(ProductId);
    }

    protected virtual void RefreshUI()
    {
        var product = IAPManager.Instance?.PurchaseModule.FindProduct(ProductId);
        if (product == null) return;

        if (titleText != null) titleText.text = product.metadata.localizedTitle;
        if (descriptionText != null) descriptionText.text = product.metadata.localizedDescription;
        if (priceText != null) priceText.text = product.metadata.localizedPriceString;
    }

    public virtual void OnClick()
    {
        if (string.IsNullOrEmpty(ProductId))
        {
            Debug.LogWarning("[IAPButton] ProductId is empty!");
            return;
        }

        IAPAnalytic.LogPurchaseBtnClick(ProductId);
        SetLoading(true);
        IAPManager.Instance?.PurchaseModule?.Purchase(ProductId);
    }

    public void SetLoadingState(bool isLoading)
    {
        if (button != null) button.interactable = !isLoading;
        if (loadingOverlay != null) loadingOverlay.SetActive(isLoading);
    }

    void SetLoading(bool isLoading)
    {
        SetLoadingState(isLoading);
    }


    /// <summary>Override để xử lý sau khi purchase confirmed và delivered.</summary>
    protected virtual void OnPurchaseSuccess(string productId, UnityEngine.Purchasing.ConfirmedOrder order)
    {
        Debug.Log("IAPButton".IAP($"Purchase success: {productId}"));
    }

    /// <summary>Override để xử lý khi mua thất bại.</summary>
    protected virtual void OnPurchaseFailed(string productId)
    {
        Debug.LogWarning("IAPButton".IAP($"Purchase failed: {productId}"));
    }
}
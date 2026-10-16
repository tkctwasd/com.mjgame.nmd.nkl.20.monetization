#if USE_IAP
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
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
        SetButtonLoading(true);
    }

    protected virtual void Start()
    {
        if (IAPManager.Instance != null && IAPManager.Instance.IsProductsReady)
            RefreshUI();
    }

    protected virtual void OnEnable()
    {
        if (IAPManager.Instance == null) return;
        IAPManager.Instance.OnProductsReady += OnProductsReady;
        IAPManager.Instance.OnProductsFetchFailed += OnProductsFetchFailed;
        IAPManager.Instance.OnProductDelivered += OnProductDelivered;
        IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        IAPManager.Instance.OnRestoreComplete += OnRestoreComplete;
    }



    protected virtual void OnDisable()
    {
        if (IAPManager.Instance == null) return;
        IAPManager.Instance.OnProductsReady -= OnProductsReady;
        IAPManager.Instance.OnProductsFetchFailed -= OnProductsFetchFailed;
        IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
        IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        IAPManager.Instance.OnRestoreComplete -= OnRestoreComplete;
    }

    void OnProductsReady(List<Product> products) => RefreshUI();

    void OnProductsFetchFailed()
    {
        Debug.LogWarning("IAPButtonUI".IAP($"Products fetch failed. Button [{ProductId}] unavailable."));
    }

    void OnProductDelivered(string productId, ConfirmedOrder order)
    {
        if (productId != ProductId) return;
        EndPurchaseFlow();
        OnPurchaseSuccess(productId, order);
    }

    void OnPurchaseFailed(FailedOrder failed)
    {

        bool mine = false;
        foreach (var item in failed.CartOrdered.Items())
            if (item.Product.definition.id == ProductId) { mine = true; break; }
        if (!mine) return;

        EndPurchaseFlow();
        OnPurchaseFailed(ProductId);
    }
    protected virtual void RefreshUI()
    {
        var product = IAPManager.Instance?.PurchaseModule.FindProduct(ProductId);
        if (product == null)
        {
            SetButtonLoading(true);
            return;
        }

        if (titleText != null) titleText.text = product.metadata.localizedTitle;
        // if (descriptionText != null) descriptionText.text = product.metadata.localizedDescription;
        if (priceText != null) priceText.text = product.metadata.localizedPriceString;

        SetButtonLoading(false);
    }

    public virtual void OnClick()
    {
        if (string.IsNullOrEmpty(ProductId))
        {
            Debug.LogWarning("[IAPButtonUI] ProductId is empty!");
            return;
        }

        IAPAnalytic.LogPurchaseBtnClick(ProductId);
        BeginPurchaseFlow();
        IAPManager.Instance?.PurchaseModule?.Purchase(ProductId);
    }

    void BeginPurchaseFlow()
    {
        SetButtonLoading(true);
        IAPManager.Instance?.NotifyPurchaseFlow(true);
    }

    void EndPurchaseFlow()
    {
        SetButtonLoading(false);
        IAPManager.Instance?.NotifyPurchaseFlow(false);
    }

    public void SetButtonLoading(bool isLoading)
    {
        if (button != null) button.interactable = !isLoading;
        if (loadingOverlay != null) loadingOverlay.SetActive(isLoading);
    }

    protected virtual void OnPurchaseSuccess(string productId, ConfirmedOrder order)
    {
        Debug.Log("IAPButtonUI".IAP($"Purchase success: {productId}"));
    }

    protected virtual void OnPurchaseFailed(string productId)
    {
        Debug.LogWarning("IAPButtonUI".IAP($"Purchase failed: {productId}"));
    }

    public void SetInteractable(bool v)
    {
        if (button != null) button.interactable = v;
    }

    public virtual void OnRestoreComplete(bool success)
    {
    }

}
#endif
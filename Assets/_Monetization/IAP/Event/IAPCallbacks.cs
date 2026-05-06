#if USE_IAP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPCallbacks
{
    readonly IAPManager m_Manager;

    public IAPCallbacks(IAPManager manager)
    {
        m_Manager = manager;
    }


    public void OnProductsFetched(List<Product> products)
    {
        Debug.Log("IAPCallbacks".IAP($"OnProductsFetched: {products.Count} products"));
        m_Manager.IsProductsReady = true;
        m_Manager.NotifyProductsReady(products);

        // Sau khi có products → fetch purchases để restore entitlement
        m_Manager.FetchExistingPurchases();
    }

    public void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogError("IAPCallbacks".IAP($"OnProductsFetchFailed: {failure.FailureReason}"));
    }


    public void OnPurchasesFetched(Orders existingOrders)
    {
        m_Manager.IsPurchasesReady = true;
        Debug.Log("IAPCallbacks".IAP(
            $"OnPurchasesFetched - Confirmed: {existingOrders.ConfirmedOrders.Count}" +
            $" | Pending: {existingOrders.PendingOrders.Count}"));

        m_Manager.NotifyPurchasesReady(existingOrders);
    }

    public void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogError("IAPCallbacks".IAP($"OnPurchasesFetchFailed: {failure.FailureReason} | {failure.Message}"));
    }

    /// <summary>
    /// Gọi khi có purchase MỚI (người dùng vừa mua) hoặc pending chưa confirm.
    /// Thay thế ProcessPurchase của IAP v4.
    /// </summary>

    public void OnPurchasePending(PendingOrder pendingOrder)
    {
        foreach (var item in pendingOrder.CartOrdered.Items())
        {
            var product = item.Product;
            Debug.Log("IAPCallbacks".IAP($"OnPurchasePending: {product.definition.id}"));

            // Track purchase attempt only. Success event is sent after confirmation.
            LogAnalyticsIAP.LogPurchaseAttempt(product.definition.id);
        }

        // Confirm ngay (auto-confirm).
        // Nếu game bạn cần server-side verify trước, hãy delay ConfirmPurchase lại.
        m_Manager.ConfirmPurchase(pendingOrder);
    }

    public void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmed:
                OnConfirmed(confirmed);
                break;
            case FailedOrder failed:
                OnConfirmFailed(failed);
                break;
        }
    }

    void OnConfirmed(ConfirmedOrder order)
    {
        m_Manager.SendIAPRevenueTracker(order);

        foreach (var item in order.CartOrdered.Items())
        {
            var productId = item.Product.definition.id;
            Debug.Log("IAPCallbacks".IAP($"OnConfirmed: {productId} | TX: {order.Info.TransactionID}"));

            // Giao hàng cho người chơi
            m_Manager.DeliverProduct(productId, order);
        }
    }

    void OnConfirmFailed(FailedOrder failed)
    {
        foreach (var item in failed.CartOrdered.Items())
        {
            Debug.LogWarning("IAPCallbacks".IAP($"ConfirmFailed: {item.Product.definition.id} | {failed.FailureReason}"));
        }
    }

    public void OnPurchaseFailed(FailedOrder failedOrder)
    {
        foreach (var item in failedOrder.CartOrdered.Items())
        {
            var productId = item.Product.definition.id;
            Debug.LogWarning("IAPCallbacks".IAP($"OnPurchaseFailed: {productId} | {failedOrder.FailureReason}"));

            LogAnalyticsIAP.LogPurchaseFailure(productId, failedOrder.FailureReason.ToString());
        }

        m_Manager.NotifyPurchaseFailed(failedOrder);
    }

    public void OnPurchaseDeferred(DeferredOrder deferredOrder)
    {
        foreach (var item in deferredOrder.CartOrdered.Items())
        {
            Debug.Log("IAPCallbacks".IAP($"OnPurchaseDeferred: {item.Product.definition.id}"));
        }
    }

    public void OnStoreDisconnected(StoreConnectionFailureDescription desc)
    {
        m_Manager.IsConnected = false;
        Debug.LogError("IAPCallbacks".IAP($"OnStoreDisconnected: {desc.isRetryable} | {desc.message}"));
    }
}
#endif
#if USE_IAP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPCallbacks
{
    readonly IAPManager InAppPurchaseManager;

    public IAPCallbacks(IAPManager manager)
    {
        InAppPurchaseManager = manager;
    }


    public void OnProductsFetched(List<Product> products)
    {
        Debug.Log("IAPCallbacks".IAP($"OnProductsFetched: {products.Count} products"));
        InAppPurchaseManager.IsProductsReady = true;
        InAppPurchaseManager.NotifyProductsReady(products);

        // Sau khi có products → fetch purchases để restore entitlement
        InAppPurchaseManager.FetchExistingPurchases();
    }

    public void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogError("IAPCallbacks".IAP($"OnProductsFetchFailed: {failure.FailureReason}"));
        InAppPurchaseManager.NotifyProductsFetchFailed();
    }


    public void OnPurchasesFetched(Orders existingOrders)
    {
        InAppPurchaseManager.IsPurchasesReady = true;
        Debug.Log("IAPCallbacks".IAP(
            $"OnPurchasesFetched - Confirmed: {existingOrders.ConfirmedOrders.Count}" +
            $" | Pending: {existingOrders.PendingOrders.Count}"));

        InAppPurchaseManager.NotifyPurchasesReady(existingOrders);
    }

    public void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogError("IAPCallbacks".IAP($"OnPurchasesFetchFailed: {failure.FailureReason} | {failure.Message}"));
    }


    public void OnPurchasePending(PendingOrder pendingOrder)
    {
        foreach (var item in pendingOrder.CartOrdered.Items())
        {
            var product = item.Product;
            Debug.Log("IAPCallbacks".IAP($"OnPurchasePending: {product.definition.id}"));

            // Track purchase attempt only. Success event is sent after confirmation.
            IAPAnalytic.LogPurchaseAttempt(product.definition.id);
        }

        // Confirm ngay (auto-confirm).
        // Nếu game bạn cần server-side verify trước, hãy delay ConfirmPurchase lại.
        InAppPurchaseManager.ConfirmPurchase(pendingOrder);
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
        InAppPurchaseManager.SendIAPRevenueTracker(order);

        foreach (var item in order.CartOrdered.Items())
        {
            var productId = item.Product.definition.id;
            Debug.Log("IAPCallbacks".IAP($"OnConfirmed: {productId} | TX: {order.Info.TransactionID}"));

            // Giao hàng cho người chơi
            InAppPurchaseManager.DeliverProduct(productId, order);
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

            IAPAnalytic.LogPurchaseFailure(productId, failedOrder.FailureReason.ToString());
        }

        InAppPurchaseManager.NotifyPurchaseFailed(failedOrder);
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
        InAppPurchaseManager.IsConnected = false;
        Debug.LogError("IAPCallbacks".IAP($"OnStoreDisconnected: {desc.isRetryable} | {desc.message}"));
    }
}
#endif
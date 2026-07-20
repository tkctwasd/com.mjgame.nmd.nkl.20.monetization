#if USE_IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPRevenueTracker
{
    public void SendAllTracker(Order order)
    {
        var transactionId = string.Empty;
        try
        {
            transactionId = order?.Info?.TransactionID ?? string.Empty;
        }
        catch
        {
            transactionId = string.Empty;
        }

        foreach (var item in order.CartOrdered.Items())
        {
            var product = item.Product;
            var price = (double)(product.metadata?.localizedPrice ?? 0);
            var currency = product.metadata?.isoCurrencyCode ?? "USD";

            LogPurchaseToFirebase(product, price, currency, transactionId);
            LogPurchaseToAdjust(product, price, currency, transactionId);
            LogPurchaseToAppsflyer(product, price, currency, transactionId);
        }
    }

    void LogPurchaseToFirebase(Product product, double price, string currency, string transactionId)
    {
        try
        {
            IAPAnalytic.LogPurchaseSuccess(product, price, currency, transactionId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPRevenueTracker] Firebase log error: {e.Message}");
        }
    }

    void LogPurchaseToAdjust(Product product, double price, string currency, string transactionId)
    {
#if USE_ADJUST
        try
        {
            var token = "ba36o4yrlp8g";
            if (string.IsNullOrEmpty(token)) return;
            var adjustEvent = new AdjustSdk.AdjustEvent(token);
            adjustEvent.SetRevenue(price, currency);
            adjustEvent.AddCallbackParameter("transaction_id", transactionId);
            adjustEvent.AddCallbackParameter("product_id", product.definition.id);
            AdjustSdk.Adjust.TrackEvent(adjustEvent);
            Debug.Log("IAPRevenueTracker".IAP($"[Adjust] {product.definition.id}"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPRevenueTracker] Adjust log error: {e.Message}");
        }
#endif
    }

    void LogPurchaseToAppsflyer(Product product, double price, string currency, string transactionId)
    {
#if USE_APPSFLYER
        try
        {
            var p = new Dictionary<string, string>
            {
                { AFInAppEvents.REVENUE, price.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                { AFInAppEvents.CURRENCY, currency },
                { AFInAppEvents.CONTENT_ID, product.definition.id },
                { AFInAppEvents.QUANTITY, "1" },
                { AFInAppEvents.ORDER_ID, transactionId }
            };
            AppsFlyerSDK.AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, p);
            Debug.Log("IAPRevenueTracker".IAP($"[Appsflyer] {product.definition.id}"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPRevenueTracker] Appsflyer log error: {e.Message}");
        }
#endif
    }
}
#endif

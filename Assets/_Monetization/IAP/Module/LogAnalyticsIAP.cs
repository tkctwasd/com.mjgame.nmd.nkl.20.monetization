using System;
using UnityEngine;
using UnityEngine.Purchasing;
using Firebase.Analytics;

public static class LogAnalyticsIAP
{
      public static void LogPurchaseAttempt(string productId)
      {
            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_attempt", new[]
            {
            new Parameter("product_id", productId)
        });

            Debug.Log("LogAnalyticsIAP".IAP($"LogPurchaseAttempt: {productId}"));
      }

      public static void LogPurchaseSuccess(Product product, double price, string currency, string transactionId = "")
      {
            var parameters = new[]
            {
            new Parameter("product_id", product.definition.id),
            new Parameter("price", price),
            new Parameter("currency", currency),
            new Parameter("transaction_id", string.IsNullOrEmpty(transactionId) ? "unknown" : transactionId)
        };

            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_success", parameters);
            Debug.Log("LogAnalyticsIAP".IAP($"LogPurchaseSuccess: {product.definition.id} | {price} {currency} | TX: {transactionId}"));
      }

      public static void LogPurchaseFailure(string productId, string failureReason)
      {

            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_failed", new[]
            {
            new Parameter("product_id", productId),
            new Parameter("failure_reason", failureReason)
        });

            Debug.LogWarning("LogAnalyticsIAP".IAP($"LogPurchaseFailure: {productId} | Reason: {failureReason}"));
      }

      public static void LogRestoreStart()
      {
            FirebaseAnalyticManager.Instance.LogEvent("iap_restore_start", new Parameter[] { });
            Debug.Log("LogAnalyticsIAP".IAP("LogRestoreStart"));
      }

      public static void LogRestoreComplete(bool success)
      {
            FirebaseAnalyticManager.Instance.LogEvent("iap_restore_complete", new[]
            {
            new Parameter("success", success ? "true" : "false")
        });

            Debug.Log("LogAnalyticsIAP".IAP($"LogRestoreComplete: {success}"));
      }
}

#if USE_IAP
using System;
using UnityEngine;
using UnityEngine.Purchasing;
using Firebase.Analytics;

public static class IAPAnalytic
{
      public static void LogPurchaseBtnClick(string productId)
      {
            if (FirebaseAnalyticManager.Instance == null)
            {
                  Debug.LogWarning("[IAPAnalytic] Firebase not ready, skip LogPurchaseBtnClick.");
                  return;
            }

            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_btn_click", new[]
            {
                  new Parameter("product_id", productId)
            });

            Debug.Log("IAPAnalytic".IAP($"LogPurchaseBtnClick: {productId}"));
      }

      public static void LogPurchaseAttempt(string productId)
      {
            if (FirebaseAnalyticManager.Instance == null)
            {
                  Debug.LogWarning("[IAPAnalytic] Firebase not ready, skip LogPurchaseAttempt.");
                  return;
            }

            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_attempt", new[]
            {
                  new Parameter("product_id", productId)
            });

            Debug.Log("IAPAnalytic".IAP($"LogPurchaseAttempt: {productId}"));
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
            Debug.Log("IAPAnalytic".IAP($"LogPurchaseSuccess: {product.definition.id} | {price} {currency} | TX: {transactionId}"));
      }

      public static void LogPurchaseFailure(string productId, string failureReason)
      {

            FirebaseAnalyticManager.Instance.LogEvent("iap_purchase_failed", new[]
            {
                  new Parameter("product_id", productId),
                  new Parameter("failure_reason", failureReason)
            });

            Debug.LogWarning("IAPAnalytic".IAP($"LogPurchaseFailure: {productId} | Reason: {failureReason}"));
      }

      public static void LogRestoreStart()
      {
            FirebaseAnalyticManager.Instance.LogEvent("iap_restore_start", new Parameter[] { });
            Debug.Log("IAPAnalytic".IAP("LogRestoreStart"));
      }

      public static void LogRestoreComplete(bool success)
      {
            FirebaseAnalyticManager.Instance.LogEvent("iap_restore_complete", new[]
            {
                  new Parameter("success", success ? "true" : "false")
            });

            Debug.Log("IAPAnalytic".IAP($"LogRestoreComplete: {success}"));
      }
}
#endif
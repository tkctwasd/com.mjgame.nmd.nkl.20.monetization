#if USE_IAP
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPPurchaseModule
{
      readonly IProductService ProductService;
      readonly IPurchaseService PurchaseService;
      readonly IAPProducts IAPProducts;
      readonly Func<bool> IsConnected;
      readonly Func<bool> IsProductsReady;
      readonly Func<bool> IsPurchasesReady;

      public IAPPurchaseModule(IProductService productService, IPurchaseService purchaseService, IAPProducts productConfig, Func<bool> isConnected, Func<bool> isProductsReady, Func<bool> isPurchasesReady)
      {
            ProductService = productService;
            PurchaseService = purchaseService;
            IAPProducts = productConfig;
            IsConnected = isConnected;
            IsProductsReady = isProductsReady;
            IsPurchasesReady = isPurchasesReady;
      }

      public bool CanPurchase => IsConnected?.Invoke() == true && IsProductsReady?.Invoke() == true;



      /// <summary>
      /// Mua một sản phẩm.
      /// Kết quả trả về qua event OnProductDelivered (success) hoặc OnPurchaseFailedEvent (fail).
      /// </summary>
      public void Purchase(string productId)
      {
            if (!CanPurchase)
            {
                  Debug.LogWarning("IAPPurchaseModule".IAP($"Not ready. Purchase skipped: {productId}"));
                  return;
            }

            var product = FindProduct(productId);
            if (product == null)
            {
                  Debug.LogWarning("IAPPurchaseModule".IAP($"Product not found: {productId}"));
                  return;
            }
            // IAPAnalytic.LogPurchaseAttempt(productId);
            PurchaseProduct(product);
            Debug.Log("IAPPurchaseModule".IAP($"PurchaseProduct: {productId}"));
      }

      public void Purchase(string productId, Action<string, bool> callback)
      {
            if (!CanPurchase)
            {
                  callback?.Invoke(productId, false);
                  return;
            }

            Action<string, ConfirmedOrder> onSuccess = null;
            Action<FailedOrder> onFail = null;

            onSuccess = (id, order) =>
            {
                  if (id != productId) return;
                  IAPManager.Instance.OnProductDelivered -= onSuccess;
                  IAPManager.Instance.OnPurchaseFailedEvent -= onFail;
                  callback?.Invoke(productId, true);
            };

            onFail = failed =>
            {
                  bool match = failed.CartOrdered.Items().Any(i => i.Product.definition.id == productId);
                  if (!match) return;
                  IAPManager.Instance.OnProductDelivered -= onSuccess;
                  IAPManager.Instance.OnPurchaseFailedEvent -= onFail;
                  callback?.Invoke(productId, false);
            };

            IAPManager.Instance.OnProductDelivered += onSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += onFail;
            Purchase(productId);
      }

      /// <summary>
      /// Tìm product theo ID trong danh sách đã fetch.
      /// </summary>
      public Product FindProduct(string productId)
      {
            return GetFetchedProducts()?.FirstOrDefault(p => p.definition.id == productId);
      }

      /// <summary>
      /// Lấy toàn bộ danh sách products đã fetch.
      /// </summary>
      public ReadOnlyObservableCollection<Product> GetFetchedProducts()
      {
            return ProductService?.GetProducts();
      }

      public void PurchaseProduct(Product product)
      {
            if (product == null)
                  return;

            PurchaseService?.PurchaseProduct(product);
      }

      /// <summary>
      /// Lấy giá hiển thị, ví dụ "$0.99".
      /// </summary>
      public string GetLocalizedPrice(string productId)
      {
            return FindProduct(productId)?.metadata?.localizedPriceString ?? string.Empty;
      }

      /// <summary>
      /// Kiểm tra Non-Consumable / Subscription đã mua chưa
      /// qua danh sách ConfirmedOrders từ lần FetchPurchases gần nhất.
      /// </summary>
      public bool IsPurchased(string productId)
      {
            if (!IsPurchasesReady?.Invoke() ?? true) return false;
            var orders = PurchaseService?.GetPurchases();
            if (orders == null)
                  return false;

            foreach (var order in orders)
            {
                  if (order is ConfirmedOrder confirmed &&
                      confirmed.CartOrdered.Items().Any(i => i.Product.definition.id == productId))
                  {
                        return true;
                  }
            }

            return false;
      }

      public string RemoveAdsProductId => IAPProducts?.RemoveAdsProductId;

      public bool IsRemoveAdsPurchased()
      {
            return !string.IsNullOrEmpty(RemoveAdsProductId) && IsPurchased(RemoveAdsProductId);
      }

      public void FetchExistingPurchases()
      {
            PurchaseService?.FetchPurchases();
      }

      public void RestorePurchases(Action<bool> callback = null)
      {
            IAPAnalytic.LogRestoreStart();
            PurchaseService?.RestoreTransactions((success, error) =>
            {
                  IAPAnalytic.LogRestoreComplete(success);
                  if (!success)
                        Debug.LogWarning($"[IAPPurchaseModule] RestorePurchases failed: {error}");
                  else
                        Debug.Log("IAPPurchaseModule".IAP("RestorePurchases success"));

                  callback?.Invoke(success);
            });
      }

      public void ConfirmPurchase(PendingOrder pendingOrder)
      {
            PurchaseService?.ConfirmPurchase(pendingOrder);
      }

      /// <summary>
      /// Xác nhận thủ công một pending order theo productId.
      /// Dùng khi tắt auto-confirm để verify server-side trước.
      /// </summary>
      public void ConfirmPendingPurchase(string productId)
      {
            var orders = PurchaseService?.GetPurchases();
            if (orders == null)
                  return;

            foreach (var order in orders)
            {
                  if (order is PendingOrder pending &&
                      pending.CartOrdered.Items().Any(i => i.Product.definition.id == productId))
                  {
                        ConfirmPurchase(pending);
                        return;
                  }
            }

            Debug.LogWarning("IAPPurchaseModule".IAP($"No pending order found for: {productId}"));
      }
}

#endif
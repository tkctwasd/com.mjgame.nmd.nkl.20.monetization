#if USE_IAP
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IAPExample : UIPanel
{
      public Button btnClose;

      [Header("Pack buttons")]
      public IAPPackButton smallPackButton;
      public IAPPackButton removeAdsPackButton;
      public IAPPackButton vipPackButton;

      [Header("Optional status")]
      public TextMeshProUGUI statusText;
      protected override void Awake()
      {
            base.Awake();
            btnClose.onClick.AddListener(OnClickBtnClose);
      }

      private void OnEnable()
      {
            AdsManager.Instance.MRECModule.ShowMREC();
            if (IAPManager.Instance != null)
                  SubscribeEvents();
      }

      private void OnClickBtnClose()
      {
            Hide();
      }

      public override void Hide()
      {
            base.Hide();
            AdsManager.Instance.MRECModule.HideMREC();
      }

      IEnumerator Start()
      {
            LoadProductsID();
            SetStatus("Waiting for IAP shop...");

            yield return new WaitUntil(() => IAPManager.Instance != null && IAPManager.Instance.IsProductsReady);

            SetStatus("Shop ready. Choose a pack.");
            SubscribeEvents();
      }


      void OnDisable()
      {
            if (IAPManager.Instance != null)
            {
                  IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
                  IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
            }
      }

      void LoadProductsID()
      {
      }

      void SubscribeEvents()
      {
            if (IAPManager.Instance == null) return;
            IAPManager.Instance.OnProductDelivered -= OnProductDelivered;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
            IAPManager.Instance.OnProductDelivered += OnProductDelivered;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
      }

      void OnProductDelivered(string productId, UnityEngine.Purchasing.ConfirmedOrder order)
      {
            if (!IsShopProduct(productId))
                  return;

            SetStatus($"Purchase confirmed: {productId}");
            SetLoading(false, productId);
      }

      void OnPurchaseFailed(UnityEngine.Purchasing.FailedOrder failedOrder)
      {
            foreach (var item in failedOrder.CartOrdered.Items())
            {
                  if (!IsShopProduct(item.Product.definition.id))
                        continue;

                  SetStatus($"Purchase failed: {item.Product.definition.id} ({failedOrder.FailureReason})");
                  SetLoading(false, item.Product.definition.id);
                  return;
            }
      }

      bool IsShopProduct(string productId)
      {
            return productId == smallPackButton.ProductId || productId == removeAdsPackButton.ProductId || productId == vipPackButton.ProductId;
      }

      void SetStatus(string message)
      {
            if (statusText != null)
                  statusText.text = message;

            Debug.Log("PanelShop".IAP(message));
      }

      void SetLoading(bool loading, string productId)
      {
      }
}

#else
using UnityEngine;
public class PanelShop : MonoBehaviour { }
#endif

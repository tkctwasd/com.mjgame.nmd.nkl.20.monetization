// using System.Collections;
// using UnityEngine;
// using UnityEngine.Purchasing;

// public class RemoveAdsButton : IAPButtonUI
// {
//     protected override IEnumerator Start()
//     {
//         // Gán productId từ project-specific config if available.
//         if (IAPManager.Instance != null)
//             ProductId = IAPManager.Instance.PurchaseModule.RemoveAdsProductId;

//         yield return base.Start();

//         // Ẩn nút nếu đã mua rồi (sau khi purchases fetch xong)
//         yield return new WaitUntil(() =>
//             IAPManager.Instance != null && IAPManager.Instance.IsPurchasesReady);

//         RefreshVisibility();
//     }

//     protected override void OnEnable()
//     {
//         base.OnEnable();
//         if (IAPManager.Instance != null)
//             IAPManager.Instance.OnPurchasesReady += OnPurchasesReady;
//     }

//     protected override void OnDisable()
//     {
//         base.OnDisable();
//         if (IAPManager.Instance != null)
//             IAPManager.Instance.OnPurchasesReady -= OnPurchasesReady;
//     }

//     void OnPurchasesReady(Orders orders) => RefreshVisibility();

//     void RefreshVisibility()
//     {
//         bool purchased = IAPManager.Instance != null && IAPManager.Instance.PurchaseModule.IsRemoveAdsPurchased();
//         gameObject.SetActive(!purchased);
//     }

//     protected override void OnPurchaseSuccess(string productId, ConfirmedOrder order)
//     {
//         base.OnPurchaseSuccess(productId, order);
//         // Tắt quảng cáo — implement tuỳ game
//         // AdsManager.Instance.SetAdsActive(false);
//         Debug.Log("RemoveAdsButton".IAP("Remove Ads purchased! Hiding ads..."));
//         gameObject.SetActive(false);
//     }
// }
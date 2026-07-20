using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPButtonRemoveAds : IAPButtonUI
{
    public bool active = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshUI();
    }
    protected override void RefreshUI()
    {
        base.RefreshUI();

        // Store báo đã sở hữu nhưng local chưa ghi (cài lại máy / restore) → ghi lại entitlement
        // để lần mở game offline sau vẫn nhận là đã mua.
        if (IAPManager.Instance != null && IAPManager.Instance.PurchaseModule != null
            && IAPManager.Instance.PurchaseModule.IsRemoveAdsPurchased())
            SaveRemoveAdsPurchased();

        if (DataSave.Instance != null && DataSave.Instance.DataMonetization.IsRemoveAdsPurchased)
        {
            if (priceText != null) priceText.text = "<color=red>Owned</color>";
            if (button != null) button.interactable = false;
            this.gameObject.SetActive(!active);
        }
    }

    /// <summary>
    /// Ghi cờ remove-ads và flush xuống đĩa ngay.
    /// DataSave chỉ tự lưu lúc pause/quit nên nếu app bị kill sau khi mua sẽ mất cờ.
    /// </summary>
    void SaveRemoveAdsPurchased()
    {
        if (DataSave.Instance == null) return;
        if (DataSave.Instance.DataMonetization.IsRemoveAdsPurchased) return;

        DataSave.Instance.DataMonetization.RemoveAdsPurchased();
        DataSave.Instance.SaveGame();
    }


    protected override void OnPurchaseSuccess(string productId, ConfirmedOrder order)
    {
        base.OnPurchaseSuccess(productId, order);
        Debug.Log("<color=green>Remove Ads purchased successfully.</color>");

        SaveRemoveAdsPurchased();

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.BannerModule.HideBanner();
            AdsManager.Instance.MRECModule.HideMREC();
        }

        RefreshUI();
    }

    protected override void OnPurchaseFailed(string productId)
    {
        base.OnPurchaseFailed(productId);
        Debug.Log("<color=red>Failed to purchase Remove Ads.</color>");
    }

    public override void OnRestoreComplete(bool success)
    {
        base.OnRestoreComplete(success);
        RefreshUI();
    }

}

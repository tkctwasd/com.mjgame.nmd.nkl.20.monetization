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

        // Việc ghi entitlement + ẩn ads do MonetizationEntitlements lo, nút này chỉ hiển thị.
        if (MonetizationEntitlements.HasRemoveAds)
        {
            if (priceText != null) priceText.text = "<color=red>Owned</color>";
            if (button != null) button.interactable = false;
            this.gameObject.SetActive(!active);
        }
    }

    protected override void OnPurchaseSuccess(string productId, ConfirmedOrder order)
    {
        base.OnPurchaseSuccess(productId, order);
        Debug.Log("<color=green>Remove Ads purchased successfully.</color>");
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

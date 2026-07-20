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
        bool alreadyPurchased = IAPManager.Instance?.PurchaseModule?.IsRemoveAdsPurchased() == true;
        if (DataSave.Instance.DataMonetization.IsRemoveAdsPurchased)
        {
            priceText.text = "<color=red>Owned</color>";
            button.interactable = false;
            this.gameObject.SetActive(!active);
        }
        else if (alreadyPurchased)
        {
            priceText.text = "<color=#0065FF>Restore</color>";
            button.interactable = false;
        }
    }


    protected override void OnPurchaseSuccess(string productId, ConfirmedOrder order)
    {
        base.OnPurchaseSuccess(productId, order);
        Debug.Log("<color=green>Remove Ads purchased successfully.</color>");

        DataSave.Instance.DataMonetization.RemoveAdsPurchased();

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.BannerModule.HideBanner();
            AdsManager.Instance.MRECModule.HideMREC();
            AdsManager.Instance.IsShowAds = false;
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

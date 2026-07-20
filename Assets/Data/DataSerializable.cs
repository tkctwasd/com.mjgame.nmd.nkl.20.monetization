using System;
using System.Collections.Generic;

#region Data Game Play

[Serializable]
public class DataGamePlay
{
    public double Money;
    public int Gem;
    public int Shield;

    public void AddMoney(double money)
    {
        Money += money;
    }
    public void AddGem(int gem)
    {
        Gem += gem;
    }
    public void AddShield(int shield)
    {
        Shield += shield;
    }

}
#endregion

#region Monetization
[Serializable]
public class DataMonetization
{
    public bool IsRemoveAdsPurchased;

    public long RemoveAdsExpiryTicks;

    // Tiền tệ nằm ở DataGamePlay. Ở đây chỉ giữ quyền lợi và sổ giao dịch.
    public List<string> GrantedTransactionIds = new();
    public List<OwnedProduct> OwnedProducts = new();

    public bool IsOwned(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return false;

        var owned = OwnedProducts.Find(x => x.ProductId == productId);
        return owned != null && owned.IsActive;
    }

    public void SetOwned(string productId, DateTime? expiry)
    {
        if (string.IsNullOrEmpty(productId)) return;

        long ticks = expiry == null
            ? long.MaxValue
            : expiry.Value.ToUniversalTime().Ticks;

        var owned = OwnedProducts.Find(x => x.ProductId == productId);
        if (owned == null)
        {
            OwnedProducts.Add(new OwnedProduct { ProductId = productId, ExpiryTicks = ticks });
            return;
        }

        if (ticks > owned.ExpiryTicks) owned.ExpiryTicks = ticks;
    }

    public DateTime? GetExpiry(string productId)
    {
        var owned = OwnedProducts.Find(x => x.ProductId == productId);
        if (owned == null || owned.ExpiryTicks == long.MaxValue) return null;

        return new DateTime(owned.ExpiryTicks, DateTimeKind.Utc);
    }

    public DataMonetization()
    {
        IsRemoveAdsPurchased = false;
        RemoveAdsExpiryTicks = 0;
    }

    public bool HasRemoveAds
    {
        get
        {
            if (IsRemoveAdsPurchased && RemoveAdsExpiryTicks == 0) return true;
            if (RemoveAdsExpiryTicks == long.MaxValue) return true;

            return RemoveAdsExpiryTicks > DateTime.UtcNow.Ticks;
        }
    }

    public void GrantRemoveAds(DateTime? expiry)
    {
        IsRemoveAdsPurchased = true;

        if (expiry == null)
        {
            RemoveAdsExpiryTicks = long.MaxValue;
            return;
        }

        long ticks = expiry.Value.ToUniversalTime().Ticks;
        if (ticks > RemoveAdsExpiryTicks) RemoveAdsExpiryTicks = ticks;
    }

    public bool WasGranted(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId)) return false;
        return GrantedTransactionIds.Contains(transactionId);
    }

    public void MarkGranted(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId)) return;
        if (GrantedTransactionIds.Contains(transactionId)) return;

        GrantedTransactionIds.Add(transactionId);
    }

    public void RemoveAdsPurchased() => GrantRemoveAds(null);
}

[Serializable]
public class OwnedProduct
{
    public string ProductId;

    public long ExpiryTicks;

    public bool IsActive =>
        ExpiryTicks == long.MaxValue || ExpiryTicks > DateTime.UtcNow.Ticks;
}
#endregion

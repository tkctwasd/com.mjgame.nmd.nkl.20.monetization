using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;


[CreateAssetMenu(fileName = "IAPProducts", menuName = "IAP/IAP Products")]
public class IAPProducts : ScriptableObject
{
    public List<IAPProduct> products = new List<IAPProduct>()
    {
            new IAPProduct("com.yourgame.money_small", ProductType.Consumable)
            {
                Title = "Small Money Pack",
                Description = "A small bundle of money for quick upgrades.",
            },
            new IAPProduct("com.yourgame.remove_ads", ProductType.NonConsumable)
            {
                RemoveAds = true,
                Title = "Remove Ads",
                Description = "Permanently remove ads from the game.",
            },
            new IAPProduct("com.yourgame.monthly_vip", ProductType.Subscription)
            {
                Title = "Monthly VIP",
                Description = "Subscribe for VIP perks for 30 days.",
            }
    };

    private void OnValidate()
    {
        foreach (var product in products)
        {
            foreach (var reward in product.Rewards)
            {
                reward.ResetValidate();
            }
        }
    }

    public IEnumerable<ProductDefinition> CreateProductDefinitions()
    {
        return products
            .Where(x => !string.IsNullOrWhiteSpace(x.ProductId))
            .Select(x => new ProductDefinition(x.ProductId, x.ProductType));
    }

    public List<IAPReward> GetRewardsByProductId(string productId)
    {
        Debug.Log($"Getting rewards for productId: {productId}");
        var product = products.FirstOrDefault(p => p.ProductId == productId);
        return product != null ? product.Rewards : new List<IAPReward>();
    }
    public string RemoveAdsProductId =>
        products.FirstOrDefault(x =>
            x.Rewards.Exists(r => r.RewardType == IAPRewardType.RemoveAds))
        ?.ProductId;
}

[Serializable]
public class IAPProduct
{
    public string ProductId;
    public ProductType ProductType;

    public bool RemoveAds = false;

    public string Title;

    [TextArea(2, 4)]
    public string Description;
    public string LocalizedPrice;

    public List<IAPReward> Rewards = new();

    public IAPProduct() { }

    public IAPProduct(string productId, ProductType productType)
    {
        ProductId = productId;
        ProductType = productType;
    }
}


public enum IAPRewardType
{
    Money,
    Gem,
    Brainrot,
    RemoveAds,
    Subscription,
    Shield,

}

[Serializable]
public class IAPReward
{
    public IAPRewardType RewardType;

    [ShowIf(nameof(RewardType), (int)IAPRewardType.Money)]
    public double Money;

    [ShowIf(nameof(RewardType), (int)IAPRewardType.Gem)]
    public int Gem;

    [ShowIf(nameof(RewardType), (int)IAPRewardType.Brainrot)]
    public GameObject prefab;
    [ShowIf(nameof(RewardType), (int)IAPRewardType.Brainrot)]
    public string BrainrotName;

    [ShowIf(nameof(RewardType), (int)IAPRewardType.Subscription)]
    public int SubscriptionDays;

    [ShowIf(nameof(RewardType), (int)IAPRewardType.Shield)]
    public int ShieldCount;

    public void ResetValidate()
    {

        if (RewardType == IAPRewardType.Brainrot && prefab != null)
        {
            BrainrotName = prefab.name;
        }
    }
}
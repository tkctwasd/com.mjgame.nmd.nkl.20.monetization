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
            new IAPProduct("com.yourgame.money_small", ProductType.Consumable),
            new IAPProduct("com.yourgame.remove_ads", ProductType.NonConsumable),
            new IAPProduct("com.yourgame.monthly_vip", ProductType.Subscription)
    };

    public IEnumerable<ProductDefinition> CreateProductDefinitions()
    {
        return products
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ProductId))
            .Select(entry => new ProductDefinition(entry.ProductId, entry.ProductType));
    }

    public string RemoveAdsProductId => products.FirstOrDefault(entry => entry.RemoveAds)?.ProductId;
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

    public IAPProduct() { }

    public IAPProduct(string productId, ProductType productType)
    {
        ProductId = productId;
        ProductType = productType;
    }
}
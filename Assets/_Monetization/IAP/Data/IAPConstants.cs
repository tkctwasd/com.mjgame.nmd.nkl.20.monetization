using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;


[CreateAssetMenu(fileName = "IAPConstants", menuName = "IAP/IAP Constants")]
public class IAPConstants : ScriptableObject
{
    public List<IAPProduct> products = new List<IAPProduct>();

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
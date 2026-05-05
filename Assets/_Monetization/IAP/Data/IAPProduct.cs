using System;
using UnityEngine;
using UnityEngine.Purchasing;

[Serializable]
public class IAPProduct
{
    public string ProductId;
    public ProductType ProductType;
    public string Title;
    public string Description;
    public string LocalizedPrice;

    public IAPProduct() { }

    public IAPProduct(string productId, ProductType productType)
    {
        ProductId = productId;
        ProductType = productType;
    }
}
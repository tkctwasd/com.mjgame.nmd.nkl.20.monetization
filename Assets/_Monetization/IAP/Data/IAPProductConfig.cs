using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "IAPProductConfig", menuName = "IAP/IAP Product Config")]
public class IAPProductConfig : ScriptableObject
{
    [Tooltip("Add one entry for each IAP product used by this project.")]
    public List<IAPProductEntry> products = new List<IAPProductEntry>();

    public IEnumerable<ProductDefinition> CreateProductDefinitions()
    {
        return products
            .Where(entry => !string.IsNullOrWhiteSpace(entry.productId))
            .Select(entry => new ProductDefinition(entry.productId, entry.productType));
    }

    public string RemoveAdsProductId => products.FirstOrDefault(entry => entry.isRemoveAdsProduct)?.productId;
}

[System.Serializable]
public class IAPProductEntry
{
    [Tooltip("Unique IAP product id used by the store and Unity IAP.")]
    public string productId;

    [Tooltip("Product type for store configuration.")]
    public ProductType productType = ProductType.Consumable;

    [Tooltip("True if this product is the remove ads product for the project.")]
    public bool isRemoveAdsProduct = false;

    [Tooltip("Optional display title used by UI if localized title is unavailable.")]
    public string displayName;

    [Tooltip("Optional description used by UI if localized description is unavailable.")]
    [TextArea(2, 4)]
    public string description;
}

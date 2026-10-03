#if USE_IAP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPCatalogModule
{
    readonly IAPProducts IAPProducts;

    public IAPCatalogModule(IAPProducts product)
    {
        IAPProducts = product;
    }

    public void BuildCatalog(ICatalogProvider catalogProvider)
    {
        var products = new List<ProductDefinition>();

        if (IAPProducts != null && IAPProducts.products.Count > 0)
        {
            products.AddRange(IAPProducts.CreateProductDefinitions());
        }
        else
        {
            Debug.LogError("IAPCatalogModule".IAP("No IAP configuration assigned. Please assign IAPProductConfig."));
        }

        catalogProvider.AddProducts(products);
        Debug.Log("IAPCatalogModule".IAP($"Catalog built: {products.Count} products"));
    }
}
#endif

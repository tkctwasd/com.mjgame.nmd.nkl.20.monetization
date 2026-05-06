#if USE_IAP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPCatalogModule
{
    readonly IAPConstants m_ProductConfig;

    public IAPCatalogModule(IAPConstants productConfig)
    {
        m_ProductConfig = productConfig;
    }

    public void BuildCatalog(ICatalogProvider catalogProvider)
    {
        var products = new List<ProductDefinition>();

        if (m_ProductConfig != null && m_ProductConfig.products.Count > 0)
        {
            products.AddRange(m_ProductConfig.CreateProductDefinitions());
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

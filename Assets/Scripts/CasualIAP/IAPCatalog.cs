using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CasualGame.IAP
{
    [CreateAssetMenu(fileName = "IAPCatalog", menuName = "Casual IAP/Product Catalog")]
    public class IAPCatalog : ScriptableObject
    {
        public List<IAPProductDefinition> products = new List<IAPProductDefinition>();

        public IEnumerable<ProductDefinition> CreateProductDefinitions()
        {
            return products
                .Where(product => !string.IsNullOrWhiteSpace(product.productId))
                .Select(product => new ProductDefinition(product.productId, product.productType));
        }

        public Dictionary<string, StoreSpecificIds> CreateStoreSpecificIds()
        {
            var map = new Dictionary<string, StoreSpecificIds>();

            foreach (var product in products)
            {
                var ids = new StoreSpecificIds();

                if (!string.IsNullOrWhiteSpace(product.googlePlayId))
                {
                    ids.Add(product.googlePlayId, UnityEngine.Purchasing.GooglePlay.Name);
                }

                if (!string.IsNullOrWhiteSpace(product.appStoreId))
                {
                    ids.Add(product.appStoreId, UnityEngine.Purchasing.AppleAppStore.Name);
                }

                if (!string.IsNullOrWhiteSpace(product.googlePlayId) || !string.IsNullOrWhiteSpace(product.appStoreId))
                {
                    map[product.productId] = ids;
                }
            }

            return map;
        }

        public IAPProductDefinition FindProduct(string productId)
        {
            return products.Find(product => product.productId == productId);
        }
    }
}

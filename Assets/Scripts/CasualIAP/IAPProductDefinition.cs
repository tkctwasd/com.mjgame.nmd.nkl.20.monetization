using UnityEngine;
using UnityEngine.Purchasing;

namespace CasualGame.IAP
{
    [CreateAssetMenu(fileName = "IAPProduct", menuName = "Casual IAP/Product Definition")]
    public class IAPProductDefinition : ScriptableObject
    {
        [Tooltip("Unique product identifier used in your game and in Unity IAP.")]
        public string productId;

        public ProductType productType = ProductType.Consumable;

        [Tooltip("Store-specific identifier for Google Play.")]
        public string googlePlayId;

        [Tooltip("Store-specific identifier for Apple App Store.")]
        public string appStoreId;

        [Tooltip("Human-friendly display name used by your UI.")]
        public string displayName;

        [TextArea(2, 5)]
        public string description;

        [Tooltip("If true, pending purchases for this product can be auto confirmed.")]
        public bool autoConfirmPendingPurchase = true;

        [Tooltip("If true, this product will be hidden until store metadata is available.")]
        public bool hideUntilAvailable = false;
    }
}

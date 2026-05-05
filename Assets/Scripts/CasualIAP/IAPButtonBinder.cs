using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace CasualGame.IAP
{
    [RequireComponent(typeof(Button))]
    public class IAPButtonBinder : MonoBehaviour
    {
        public string productId;
        public Text titleText;
        public Text descriptionText;
        public Text priceText;
        public GameObject loadingOverlay;

        Button m_Button;
        IAPManager m_Manager;

        void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(OnClick);
        }

        void OnEnable()
        {
            m_Manager = IAPManager.Instance;
            if (m_Manager != null)
            {
                m_Manager.OnProductLoaded += OnProductLoaded;
                m_Manager.OnProductLoadFailed += OnProductLoadFailed;
            }

            UpdateUI();
        }

        void OnDisable()
        {
            if (m_Manager != null)
            {
                m_Manager.OnProductLoaded -= OnProductLoaded;
                m_Manager.OnProductLoadFailed -= OnProductLoadFailed;
            }
        }

        void OnClick()
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return;
            }

            IAPManager.Instance?.PurchaseProduct(productId);
        }

        void OnProductLoaded(IAPProductDefinition productDefinition, Product product)
        {
            if (productDefinition != null && productDefinition.productId == productId)
            {
                UpdateUI();
            }
        }

        void OnProductLoadFailed(string error)
        {
            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(false);
            }
        }

        void UpdateUI()
        {
            if (string.IsNullOrWhiteSpace(productId) || m_Manager == null)
            {
                return;
            }

            var productDefinition = m_Manager.GetProductDefinition(productId);
            var product = m_Manager.GetStoreProduct(productId);

            if (product != null)
            {
                if (titleText != null)
                {
                    titleText.text = string.IsNullOrWhiteSpace(product.metadata.localizedTitle)
                        ? productDefinition?.displayName
                        : product.metadata.localizedTitle;
                }

                if (descriptionText != null)
                {
                    descriptionText.text = string.IsNullOrWhiteSpace(product.metadata.localizedDescription)
                        ? productDefinition?.description
                        : product.metadata.localizedDescription;
                }

                if (priceText != null)
                {
                    priceText.text = product.metadata.localizedPriceString;
                }

                if (loadingOverlay != null)
                {
                    loadingOverlay.SetActive(false);
                }

                m_Button.interactable = true;
            }
            else
            {
                if (loadingOverlay != null)
                {
                    loadingOverlay.SetActive(true);
                }

                m_Button.interactable = false;
            }
        }
    }
}

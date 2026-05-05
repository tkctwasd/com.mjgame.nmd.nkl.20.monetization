using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace CasualGame.IAP
{
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        public IAPCatalog catalog;
        public bool initializeOnAwake = true;
        public bool debugLogs = true;

        public event Action OnInitializeSucceeded;
        public event Action<string> OnInitializeFailed;
        public event Action<IAPProductDefinition, Product> OnProductLoaded;
        public event Action<string> OnProductLoadFailed;
        public event Action<IAPProductDefinition, Product, IOrderInfo> OnPurchaseCompleted;
        public event Action<IAPProductDefinition, Product, PurchaseFailureReason, string> OnPurchaseFailed;
        public event Action<IAPProductDefinition, Product> OnPurchaseDeferred;
        public event Action<IAPProductDefinition, Product> OnPurchasePending;
        public event Action<Orders> OnExistingPurchasesFetched;
        public event Action OnRestoreCompleted;
        public event Action<string> OnStoreConnected;

        IStoreService m_StoreService;
        IProductService m_ProductService;
        IPurchaseService m_PurchasingService;
        ICatalogProvider m_CatalogProvider = new CatalogProvider();
        CrossPlatformValidator m_CrossPlatformValidator;

        readonly Dictionary<string, Product> m_FetchedProducts = new Dictionary<string, Product>();
        readonly Dictionary<string, IAPProductDefinition> m_ProductLookup = new Dictionary<string, IAPProductDefinition>();

        public bool IsInitialized { get; private set; }

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (catalog == null)
            {
                LogError("IAPManager requires a catalog asset to initialize.");
                OnInitializeFailed?.Invoke("Missing IAP catalog.");
                return;
            }

            BuildProductLookup();
            CreateServices();
            InitializeIapService();
            CreateCrossPlatformValidator();
            ConnectToStore();
        }

        void BuildProductLookup()
        {
            m_ProductLookup.Clear();

            foreach (var product in catalog.products)
            {
                if (string.IsNullOrWhiteSpace(product.productId))
                {
                    continue;
                }

                if (!m_ProductLookup.ContainsKey(product.productId))
                {
                    m_ProductLookup[product.productId] = product;
                }
            }
        }

        void CreateServices()
        {
            m_StoreService = UnityIAPServices.DefaultStore();
            m_ProductService = UnityIAPServices.DefaultProduct();
            m_PurchasingService = UnityIAPServices.DefaultPurchase();

            ConfigureServiceCallbacks();
            m_CatalogProvider.AddProducts(catalog.CreateProductDefinitions().ToList(), catalog.CreateStoreSpecificIds());
        }

        void ConfigureServiceCallbacks()
        {
            m_ProductService.OnProductsFetched += OnInitialProductsFetched;
            m_ProductService.OnProductsFetchFailed += OnInitialProductsFetchFailed;

            m_PurchasingService.OnPurchasesFetched += OnExistingPurchasesFetchedInternal;
            m_PurchasingService.OnPurchasesFetchFailed += OnExistingPurchasesFetchFailed;
            m_PurchasingService.OnPurchasePending += OnPurchasePendingInternal;
            m_PurchasingService.OnPurchaseConfirmed += OnPurchaseConfirmedInternal;
            m_PurchasingService.OnPurchaseFailed += OnPurchaseFailedInternal;
            m_PurchasingService.OnPurchaseDeferred += OnPurchaseDeferredInternal;
        }

        void InitializeIapService()
        {
            IAPInitializer.Initialize(OnServiceInitialized, message =>
            {
                LogError($"IAP initialization failed: {message}");
                OnInitializeFailed?.Invoke(message);
            });
        }

        void CreateCrossPlatformValidator()
        {
#if !UNITY_EDITOR
            try
            {
                if (CanCrossPlatformValidate())
                {
#if !DEBUG_STOREKIT_TEST
                    m_CrossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#else
                    m_CrossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
#endif
                }
            }
            catch (NotImplementedException exception)
            {
                LogWarning($"Cross platform validation is not supported in this build: {exception.Message}");
            }
#endif
        }

        void OnServiceInitialized()
        {
            InitializeUI();
            IsInitialized = true;
            OnInitializeSucceeded?.Invoke();
        }

        protected void InitializeUI()
        {
            // Keep this method for optional UI initialization in your game.
        }

        async void ConnectToStore()
        {
            await m_StoreService.Connect();
            Log("Store Connected.");
            OnStoreConnected?.Invoke("Store Connected.");
            FetchInitialProducts();
        }

        void FetchInitialProducts()
        {
            m_CatalogProvider.FetchProducts(m_ProductService.FetchProductsWithNoRetries, DefaultStoreHelper.GetDefaultStoreName());
        }

        public void PurchaseProduct(string productId)
        {
            if (!m_FetchedProducts.TryGetValue(productId, out var product))
            {
                LogError($"Purchase failed: product '{productId}' is not available.");
                return;
            }

            m_PurchasingService?.PurchaseProduct(product);
        }

        public Product GetStoreProduct(string productId)
        {
            m_FetchedProducts.TryGetValue(productId, out var product);
            return product;
        }

        public IAPProductDefinition GetProductDefinition(string productId)
        {
            m_ProductLookup.TryGetValue(productId, out var definition);
            return definition;
        }

        public ReadOnlyCollection<Product> GetFetchedProducts()
        {
            return m_FetchedProducts.Values.ToList().AsReadOnly();
        }

        public void RestorePurchases()
        {
            m_PurchasingService.RestoreTransactions(OnTransactionsRestored);
        }

        void OnTransactionsRestored(bool success, string error)
        {
            Log($"Restore completed: {success}");
            OnRestoreCompleted?.Invoke();
        }

        public void FetchExistingPurchases()
        {
            m_PurchasingService.FetchPurchases();
        }

        public static bool IsReceiptAvailable(Orders existingOrders)
        {
            return existingOrders != null &&
                   (existingOrders.ConfirmedOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)) ||
                    existingOrders.PendingOrders.Any(order => !string.IsNullOrEmpty(order.Info.Receipt)));
        }

        void OnInitialProductsFetched(List<Product> products)
        {
            m_FetchedProducts.Clear();

            foreach (var product in products)
            {
                m_FetchedProducts[product.definition.id] = product;

                var productDefinition = GetProductDefinition(product.definition.id);
                OnProductLoaded?.Invoke(productDefinition, product);
            }

            if (products.Count == 0)
            {
                OnProductLoadFailed?.Invoke("No IAP products were fetched from the store.");
            }

            FetchExistingPurchases();
        }

        void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            LogError($"Product fetch failed: {failure.FailureReason}");
            OnProductLoadFailed?.Invoke(failure.FailureReason);
        }

        void OnExistingPurchasesFetchedInternal(Orders existingOrders)
        {
            Log("Existing purchases fetched.");
            OnExistingPurchasesFetched?.Invoke(existingOrders);
        }

        void OnExistingPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            LogError($"Existing purchases fetch failed: {failure.Message}");
            OnProductLoadFailed?.Invoke(failure.Message);
        }

        void OnPurchasePendingInternal(PendingOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;
                var productDefinition = GetProductDefinition(product.definition.id);
                OnPurchasePending?.Invoke(productDefinition, product);
                ValidatePurchaseIfPossible(order.Info);
            }

            ConfirmOrderIfAutomatic(order);
        }

        void ConfirmOrderIfAutomatic(PendingOrder order)
        {
            if (ShouldConfirmOrderAutomatically(order))
            {
                ConfirmOrder(order);
            }
        }

        bool ShouldConfirmOrderAutomatically(PendingOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var productDefinition = GetProductDefinition(cartItem.Product.definition.id);
                if (productDefinition == null || !productDefinition.autoConfirmPendingPurchase)
                {
                    Log("Pending purchase will not be auto-confirmed for product " + cartItem.Product.definition.id);
                    return false;
                }
            }

            return true;
        }

        void OnPurchaseConfirmedInternal(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    foreach (var cartItem in confirmedOrder.CartOrdered.Items())
                    {
                        var product = cartItem.Product;
                        var productDefinition = GetProductDefinition(product.definition.id);
                        OnPurchaseCompleted?.Invoke(productDefinition, product, confirmedOrder.Info);
                    }
                    break;
                case FailedOrder failedOrder:
                    HandleFailedConfirmation(failedOrder);
                    break;
            }
        }

        void HandleFailedConfirmation(FailedOrder failedOrder)
        {
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                var productDefinition = GetProductDefinition(product.definition.id);
                OnPurchaseFailed?.Invoke(productDefinition, product, failedOrder.FailureReason, failedOrder.FailureReason.ToString());
            }
        }

        void OnPurchaseFailedInternal(FailedOrder failedOrder)
        {
            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                var productDefinition = GetProductDefinition(product.definition.id);
                OnPurchaseFailed?.Invoke(productDefinition, product, failedOrder.FailureReason, failedOrder.FailureReason.ToString());
            }
        }

        void OnPurchaseDeferredInternal(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                var product = cartItem.Product;
                var productDefinition = GetProductDefinition(product.definition.id);
                OnPurchaseDeferred?.Invoke(productDefinition, product);
            }
        }

        public void ConfirmPendingPurchaseForId(string productId)
        {
            if (!m_FetchedProducts.TryGetValue(productId, out var product))
            {
                return;
            }

            var order = GetPendingOrder(product);
            if (order != null)
            {
                ConfirmOrder(order);
            }
        }

        PendingOrder GetPendingOrder(Product product)
        {
            var orders = m_PurchasingService.GetPurchases();
            foreach (var order in orders)
            {
                if (order is PendingOrder pendingOrder &&
                    pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product.definition.storeSpecificId == product.definition.storeSpecificId)
                {
                    return pendingOrder;
                }
            }

            return null;
        }

        public void ValidatePurchaseIfPossible(IOrderInfo orderInfo)
        {
            if (CanCrossPlatformValidate())
            {
                ValidatePurchase(orderInfo);
            }
        }

        bool CanCrossPlatformValidate()
        {
            return IsGooglePlay() ||
                   Application.platform == RuntimePlatform.IPhonePlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.tvOS;
        }

        void ValidatePurchase(IOrderInfo orderInfo)
        {
            if (m_CrossPlatformValidator == null)
            {
                LogWarning("Receipt validator is not available for this platform.");
                return;
            }

            try
            {
                var result = m_CrossPlatformValidator.Validate(orderInfo.Receipt);
                Log("Receipt validated successfully.");

                if (IsGooglePlay())
                {
                    foreach (IPurchaseReceipt productReceipt in result)
                    {
                        Log($"Validated receipt for product {productReceipt.productID}.");
                    }
                }
            }
            catch (IAPSecurityException ex)
            {
                LogError($"Invalid receipt: {ex}");
            }
        }

        bool IsGooglePlay()
        {
            return Application.platform == RuntimePlatform.Android && DefaultStoreHelper.GetDefaultStoreName() == UnityEngine.Purchasing.GooglePlay.Name;
        }

        void ConfirmOrder(PendingOrder pendingOrder)
        {
            m_PurchasingService.ConfirmPurchase(pendingOrder);
        }

        void Log(string message)
        {
            if (debugLogs)
            {
                Debug.Log($"[IAPManager] {message}");
            }
        }

        void LogWarning(string message)
        {
            if (debugLogs)
            {
                Debug.LogWarning($"[IAPManager] {message}");
            }
        }

        void LogError(string message)
        {
            Debug.LogError($"[IAPManager] {message}");
        }
    }
}

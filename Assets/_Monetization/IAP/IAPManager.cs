#if USE_IAP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;

    public IAPProducts IAPProducts;
    IStoreService StoreService;
    IProductService ProductService;
    IPurchaseService PurchaseService;
    readonly ICatalogProvider CatalogProvider = new CatalogProvider();
    public IAPPurchaseModule PurchaseModule { get; private set; }
    IAPCatalogModule CatalogModule;
    IAPRevenueTracker RevenueTrackerModule;
    IAPCallbacks m_Callbacks;
    public bool IsConnected { get; internal set; }
    public bool IsProductsReady { get; internal set; }
    public bool IsPurchasesReady { get; internal set; }
    public event Action<List<Product>> OnProductsReady;
    public event Action<Orders> OnPurchasesReady;
    public event Action<string, ConfirmedOrder> OnProductDelivered; 
    public event Action<FailedOrder> OnPurchaseFailedEvent;
    public event Action<bool> OnRestoreComplete;

    void Awake()
    {
        Instance = this;
        m_Callbacks = new IAPCallbacks(this);
        CreateServices();
        CreateModules();
    }

    void Start()
    {
        IAPService.Initialize(
            onSuccess: OnUnityServicesReady,
            onError: msg => Debug.LogError("IAPManager".IAP($"Unity Services failed: {msg}"))
        );
    }

    void CreateModules()
    {
        CatalogModule = new IAPCatalogModule(IAPProducts);
        PurchaseModule = new IAPPurchaseModule(
            ProductService,
            PurchaseService,
            IAPProducts,
            () => IsConnected,
            () => IsProductsReady,
            () => IsPurchasesReady
        );
        RevenueTrackerModule = new IAPRevenueTracker();
    }

    void OnUnityServicesReady()
    {
        BuildCatalog();
        ConnectToStore();
    }

    void CreateServices()
    {
        StoreService = UnityIAPServices.DefaultStore();
        ProductService = UnityIAPServices.DefaultProduct();
        PurchaseService = UnityIAPServices.DefaultPurchase();

        // ── ProductService events ──
        ProductService.OnProductsFetched += m_Callbacks.OnProductsFetched;
        ProductService.OnProductsFetchFailed += m_Callbacks.OnProductsFetchFailed;

        // ── PurchaseService events ──
        PurchaseService.OnPurchasesFetched += m_Callbacks.OnPurchasesFetched;
        PurchaseService.OnPurchasesFetchFailed += m_Callbacks.OnPurchasesFetchFailed;
        PurchaseService.OnPurchasePending += m_Callbacks.OnPurchasePending;
        PurchaseService.OnPurchaseConfirmed += m_Callbacks.OnPurchaseConfirmed;
        PurchaseService.OnPurchaseFailed += m_Callbacks.OnPurchaseFailed;
        PurchaseService.OnPurchaseDeferred += m_Callbacks.OnPurchaseDeferred;

        // ── StoreService events ──
        StoreService.OnStoreDisconnected += m_Callbacks.OnStoreDisconnected;

        Debug.Log("IAPManager".IAP("Services created and callbacks registered"));
    }
    void BuildCatalog()
    {
        CatalogModule?.BuildCatalog(CatalogProvider);
    }

    async void ConnectToStore()
    {
        Debug.Log("IAPManager".IAP("Connecting to store..."));
        await StoreService.Connect();

        IsConnected = true;
        Debug.Log("IAPManager".IAP("Store Connected!"));

        // Fetch products qua CatalogProvider (đúng pattern sample v5)
        CatalogProvider.FetchProducts(
            ProductService.FetchProductsWithNoRetries,
            DefaultStoreHelper.GetDefaultStoreName()
        );
        Debug.Log("IAPManager".IAP("FetchProducts requested"));
    }

    internal void NotifyProductsReady(List<Product> products)
    {
        OnProductsReady?.Invoke(products);
    }

    internal void NotifyPurchasesReady(Orders orders)
    {
        OnPurchasesReady?.Invoke(orders);
    }

    internal void NotifyPurchaseFailed(FailedOrder failed)
    {
        OnPurchaseFailedEvent?.Invoke(failed);
    }

    /// <summary>
    /// Giao sản phẩm cho người chơi sau khi purchase confirmed.
    /// Game logic lắng nghe qua event OnProductDelivered.
    /// </summary>
    internal void DeliverProduct(string productId, ConfirmedOrder order)
    {
        OnProductDelivered?.Invoke(productId, order);
    }

    internal void ConfirmPurchase(PendingOrder pendingOrder)
    {
        PurchaseModule?.ConfirmPurchase(pendingOrder);
    }

    internal void FetchExistingPurchases()
    {
        PurchaseModule?.FetchExistingPurchases();
    }
    internal void SendIAPRevenueTracker(Order order)
    {
        RevenueTrackerModule?.SendAllTracker(order);
    }

}
#else

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;
    public IAPProductConfig IAPProductConfig;
    public IAPPurchaseModule PurchaseModule { get; private set; }

    public string RemoveAdsProductId => IAPProductConfig?.RemoveAdsProductId;

    public bool IsConnected      => false;
    public bool IsProductsReady  => false;
    public bool IsPurchasesReady => false;

    public event Action<List<Product>>              OnProductsReady;
    public event Action<Orders>                     OnPurchasesReady;
    public event Action<string, ConfirmedOrder>     OnProductDelivered;
    public event Action<FailedOrder>                OnPurchaseFailedEvent;
    public event Action<bool>                       OnRestoreComplete;

    void Awake()
    {
        Instance = this;
        PurchaseModule = new IAPPurchaseModule(null, null, null, () => false, () => false, () => false);
    }

    public void Purchase(string productId)
        => Debug.LogWarning("IAPManager".IAP("USE_IAP not defined. Purchase skipped."));

    public void Purchase(string productId, Action<string, bool> callback)
    {
        Debug.LogWarning("IAPManager".IAP("USE_IAP not defined. Purchase skipped."));
        callback?.Invoke(productId, false);
    }

    public Product FindProduct(string productId)                         => null;
    public ReadOnlyObservableCollection<Product> GetFetchedProducts()   => null;
    public string GetLocalizedPrice(string productId)                   => "";
    public bool IsPurchased(string productId)                           => false;
    public bool IsRemoveAdsPurchased()                                  => false;
    public void RestorePurchases(Action<bool> callback = null)          => callback?.Invoke(false);
    public void ConfirmPendingPurchase(string productId)                { }
}

#endif
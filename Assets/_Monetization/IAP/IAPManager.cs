// ─────────────────────────────────────────────────────────────────────────────
//  IAPManager.cs  –  Unity IAP v5
//
//  Dựa theo pattern chính thức của Unity IAP v5 Sample (PaywallManager).
//  Tách thành 3 service riêng biệt: IStoreService, IProductService, IPurchaseService.
//
//  Bật IAP: thêm "USE_IAP" vào Project Settings > Player > Scripting Define Symbols
//  Không có define → Stub an toàn ở cuối file, game vẫn chạy bình thường.
// ─────────────────────────────────────────────────────────────────────────────

#if USE_IAP
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;

    public IAPConstants IAPConstants;
    public IAPProductConfig IAPProductConfig;

    // ── IAP v5 Services (tách riêng thay vì 1 StoreController duy nhất) ──
    IStoreService m_StoreService;
    IProductService m_ProductService;
    IPurchaseService m_PurchaseService;

    // Catalog provider — tương đương ConfigurationBuilder của v4
    readonly ICatalogProvider m_CatalogProvider = new CatalogProvider();

    // Callbacks tách riêng class
    IAPCallbacks m_Callbacks;

    // ── State ──
    public bool IsConnected { get; internal set; }
    public bool IsProductsReady { get; internal set; }
    public bool IsPurchasesReady { get; internal set; }

    // ── Events để UI / game logic lắng nghe ──
    public event Action<List<Product>> OnProductsReady;
    public event Action<Orders> OnPurchasesReady;
    public event Action<string, ConfirmedOrder> OnProductDelivered;  // (productId, order)
    public event Action<FailedOrder> OnPurchaseFailedEvent;
    public event Action<bool> OnRestoreComplete;

    #region ── Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        m_Callbacks = new IAPCallbacks(this);
        CreateServices();
        LoadConfigIfMissing();
    }

    void Start()
    {
        IAPService.Initialize(
            onSuccess: OnUnityServicesReady,
            onError: msg => Debug.LogError("IAPManager".IAP($"Unity Services failed: {msg}"))
        );
    }

    void LoadConfigIfMissing()
    {
        if (IAPProductConfig == null)
        {
            IAPProductConfig = Resources.Load<IAPProductConfig>("IAPProductConfig");
            if (IAPProductConfig != null)
            {
                Debug.Log("IAPManager".IAP("Loaded project-specific IAPProductConfig from Resources."));
            }
        }
    }

    void OnUnityServicesReady()
    {
        BuildCatalog();
        ConnectToStore();
    }

    #endregion

    #region ── Services Setup ─────────────────────────────────────────────────

    /// <summary>
    /// Tạo 3 service riêng biệt theo pattern chính thức v5.
    /// Mỗi service có lifecycle và event riêng.
    /// </summary>
    void CreateServices()
    {
        m_StoreService = UnityIAPServices.DefaultStore();
        m_ProductService = UnityIAPServices.DefaultProduct();
        m_PurchaseService = UnityIAPServices.DefaultPurchase();

        // ── ProductService events ──
        m_ProductService.OnProductsFetched += m_Callbacks.OnProductsFetched;
        m_ProductService.OnProductsFetchFailed += m_Callbacks.OnProductsFetchFailed;

        // ── PurchaseService events ──
        m_PurchaseService.OnPurchasesFetched += m_Callbacks.OnPurchasesFetched;
        m_PurchaseService.OnPurchasesFetchFailed += m_Callbacks.OnPurchasesFetchFailed;
        m_PurchaseService.OnPurchasePending += m_Callbacks.OnPurchasePending;
        m_PurchaseService.OnPurchaseConfirmed += m_Callbacks.OnPurchaseConfirmed;
        m_PurchaseService.OnPurchaseFailed += m_Callbacks.OnPurchaseFailed;
        m_PurchaseService.OnPurchaseDeferred += m_Callbacks.OnPurchaseDeferred;

        // ── StoreService events ──
        m_StoreService.OnStoreDisconnected += m_Callbacks.OnStoreDisconnected;

        Debug.Log("IAPManager".IAP("Services created and callbacks registered"));
    }

    /// <summary>
    /// Khai báo catalog products từ IAPConstants.
    /// Tương đương ConfigurationBuilder.AddProduct() của v4.
    /// </summary>
    void BuildCatalog()
    {
        var products = new List<ProductDefinition>();

        if (IAPProductConfig != null && IAPProductConfig.products.Count > 0)
        {
            products.AddRange(IAPProductConfig.CreateProductDefinitions());
        }
        else if (IAPConstants != null)
        {
            // Fallback legacy config
            products.Add(new ProductDefinition(IAPConstants.RemoveAdsId, ProductType.NonConsumable));
            foreach (var id in IAPConstants.ConsumableIds)
                products.Add(new ProductDefinition(id, ProductType.Consumable));
            foreach (var id in IAPConstants.SubscriptionIds)
                products.Add(new ProductDefinition(id, ProductType.Subscription));
        }
        else
        {
            Debug.LogError("IAPManager".IAP("No IAP configuration assigned. Please assign IAPProductConfig or IAPConstants."));
        }

        m_CatalogProvider.AddProducts(products);
        Debug.Log("IAPManager".IAP($"Catalog built: {products.Count} products"));
    }

    /// <summary>
    /// Kết nối tới store rồi fetch products.
    /// Connect() xong mới gọi FetchProducts.
    /// </summary>
    async void ConnectToStore()
    {
        Debug.Log("IAPManager".IAP("Connecting to store..."));
        await m_StoreService.Connect();

        IsConnected = true;
        Debug.Log("IAPManager".IAP("Store Connected!"));

        // Fetch products qua CatalogProvider (đúng pattern sample v5)
        m_CatalogProvider.FetchProducts(
            m_ProductService.FetchProductsWithNoRetries,
            DefaultStoreHelper.GetDefaultStoreName()
        );
        Debug.Log("IAPManager".IAP("FetchProducts requested"));
    }

    #endregion

    #region ── Internal Callbacks (gọi từ IAPCallbacks) ──────────────────────

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
        m_PurchaseService?.ConfirmPurchase(pendingOrder);
    }

    internal void FetchExistingPurchases()
    {
        m_PurchaseService?.FetchPurchases();
    }

    #endregion

    #region ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Mua một sản phẩm.
    /// Kết quả trả về qua event OnProductDelivered (success) hoặc OnPurchaseFailedEvent (fail).
    /// </summary>
    public void Purchase(string productId)
    {
        if (!IsConnected || !IsProductsReady)
        {
            Debug.LogWarning("IAPManager".IAP($"Not ready. Purchase skipped: {productId}"));
            return;
        }

        var product = FindProduct(productId);
        if (product == null)
        {
            Debug.LogWarning("IAPManager".IAP($"Product not found: {productId}"));
            return;
        }

        FirebaseAnalyticManager.Instance?.LogAd("iap_purchase_attempt", productId);
        m_PurchaseService?.PurchaseProduct(product);
        Debug.Log("IAPManager".IAP($"PurchaseProduct: {productId}"));
    }

    /// <summary>
    /// Overload tiện lợi có callback trực tiếp.
    /// Tự đăng ký / huỷ đăng ký event một lần.
    /// </summary>
    public void Purchase(string productId, Action<string, bool> callback)
    {
        if (!IsConnected || !IsProductsReady)
        {
            callback?.Invoke(productId, false);
            return;
        }

        Action<string, ConfirmedOrder> onSuccess = null;
        Action<FailedOrder> onFail = null;

        onSuccess = (id, order) =>
        {
            if (id != productId) return;
            OnProductDelivered -= onSuccess;
            OnPurchaseFailedEvent -= onFail;
            callback?.Invoke(productId, true);
        };

        onFail = failed =>
        {
            bool match = failed.CartOrdered.Items().Any(i => i.Product.definition.id == productId);
            if (!match) return;
            OnProductDelivered -= onSuccess;
            OnPurchaseFailedEvent -= onFail;
            callback?.Invoke(productId, false);
        };

        OnProductDelivered += onSuccess;
        OnPurchaseFailedEvent += onFail;

        Purchase(productId);
    }

    /// <summary>
    /// Tìm product theo ID trong danh sách đã fetch.
    /// </summary>
    public Product FindProduct(string productId)
    {
        return GetFetchedProducts()?.FirstOrDefault(p => p.definition.id == productId);
    }

    /// <summary>
    /// Lấy toàn bộ danh sách products đã fetch.
    /// </summary>
    public ReadOnlyObservableCollection<Product> GetFetchedProducts()
    {
        return m_ProductService?.GetProducts();
    }

    /// <summary>
    /// Lấy giá hiển thị, ví dụ "$0.99".
    /// </summary>
    public string GetLocalizedPrice(string productId)
    {
        return FindProduct(productId)?.metadata?.localizedPriceString ?? "";
    }

    /// <summary>
    /// Kiểm tra Non-Consumable / Subscription đã mua chưa
    /// qua danh sách ConfirmedOrders từ lần FetchPurchases gần nhất.
    /// </summary>
    public bool IsPurchased(string productId)
    {
        if (!IsPurchasesReady) return false;
        var orders = m_PurchaseService?.GetPurchases();
        if (orders == null) return false;

        foreach (var order in orders)
        {
            if (order is ConfirmedOrder confirmed)
            {
                if (confirmed.CartOrdered.Items()
                    .Any(i => i.Product.definition.id == productId))
                    return true;
            }
        }
        return false;
    }

    /// <summary>Kiểm tra nhanh Remove Ads.</summary>
    public string RemoveAdsProductId =>
        IAPProductConfig?.RemoveAdsProductId ?? IAPConstants?.RemoveAdsId;

    public bool IsRemoveAdsPurchased() => !string.IsNullOrEmpty(RemoveAdsProductId) && IsPurchased(RemoveAdsProductId);

    /// <summary>
    /// Restore purchases (iOS bắt buộc).
    /// IAP v5: dùng RestoreTransactions của PurchaseService.
    /// </summary>
    public void RestorePurchases(Action<bool> callback = null)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("IAPManager".IAP("RestorePurchases: Not connected"));
            callback?.Invoke(false);
            return;
        }

        Debug.Log("IAPManager".IAP("RestorePurchases..."));
        m_PurchaseService?.RestoreTransactions((success, error) =>
        {
            if (!success)
                Debug.LogWarning($"[IAPManager] RestorePurchases failed: {error}");
            else
                Debug.Log("IAPManager".IAP("RestorePurchases success"));

            OnRestoreComplete?.Invoke(success);
            callback?.Invoke(success);
        });
    }

    /// <summary>
    /// Xác nhận thủ công một pending order theo productId.
    /// Dùng khi tắt auto-confirm để verify server-side trước.
    /// </summary>
    public void ConfirmPendingPurchase(string productId)
    {
        var orders = m_PurchaseService?.GetPurchases();
        if (orders == null) return;

        foreach (var order in orders)
        {
            if (order is PendingOrder pending &&
                pending.CartOrdered.Items()
                    .Any(i => i.Product.definition.id == productId))
            {
                ConfirmPurchase(pending);
                return;
            }
        }

        Debug.LogWarning("IAPManager".IAP($"No pending order found for: {productId}"));
    }

    #endregion

    #region ── Analytics ─────────────────────────────────────────────────────

    internal void LogPurchaseAnalytics(PendingOrder order)
    {
        foreach (var item in order.CartOrdered.Items())
        {
            var product = item.Product;
            var price = (double)(product.metadata?.localizedPrice ?? 0);
            var currency = product.metadata?.isoCurrencyCode ?? "USD";

            LogPurchaseToFirebase(product, price, currency);
            LogPurchaseToAdjust(product, price, currency);
            LogPurchaseToAppsflyer(product, price, currency);
        }
    }

    void LogPurchaseToFirebase(Product product, double price, string currency)
    {
        try
        {
            LogEventParameter.Create("iap_purchase_success")
                .AddParam("product_id", product.definition.id)
                .AddParam("price", price)
                .AddParam("currency", currency)
                .Send();
            Debug.Log("IAPManager".IAP($"[Firebase] {product.definition.id} | {price} {currency}"));
        }
        catch (Exception e) { Debug.LogError($"[IAPManager] Firebase log error: {e.Message}"); }
    }

    void LogPurchaseToAdjust(Product product, double price, string currency)
    {
#if USE_ADJUST
        try
        {
            // Thay "iap_adjust_token" bằng event token thật từ Adjust dashboard
            var adjustEvent = new AdjustSdk.AdjustEvent("iap_adjust_token");
            adjustEvent.SetRevenue(price, currency);
            AdjustSdk.Adjust.TrackEvent(adjustEvent);
            Debug.Log("IAPManager".IAP($"[Adjust] {product.definition.id}"));
        }
        catch (Exception e) { Debug.LogError($"[IAPManager] Adjust log error: {e.Message}"); }
#endif
    }

    void LogPurchaseToAppsflyer(Product product, double price, string currency)
    {
        // #if USE_APPSFLYER
        //         try
        //         {
        //             var p = new Dictionary<string, string>
        //             {
        //                 { AppsFlyerSDK.AFInAppEvents.REVENUE,    price.ToString() },
        //                 { AppsFlyerSDK.AFInAppEvents.CURRENCY,   currency },
        //                 { AppsFlyerSDK.AFInAppEvents.CONTENT_ID, product.definition.id },
        //                 { AppsFlyerSDK.AFInAppEvents.QUANTITY,   "1" }
        //             };
        //             AppsFlyerSDK.AppsFlyer.sendEvent(AppsFlyerSDK.AFInAppEvents.PURCHASE, p);
        //             Debug.Log("IAPManager".IAP($"[Appsflyer] {product.definition.id}"));
        //         }
        //         catch (Exception e) { Debug.LogError($"[IAPManager] Appsflyer log error: {e.Message}"); }
        // #endif
    }

    #endregion
}

#else

// ─────────────────────────────────────────────────────────────────────────────
//  STUB – không có define "USE_IAP"
//  Mọi lời gọi vẫn biên dịch và trả về giá trị an toàn.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;
    public IAPConstants IAPConstants;
    public IAPProductConfig IAPProductConfig;

    public string RemoveAdsProductId => IAPProductConfig?.RemoveAdsProductId ?? IAPConstants?.RemoveAdsId;

    public bool IsConnected      => false;
    public bool IsProductsReady  => false;
    public bool IsPurchasesReady => false;

    public event Action<List<Product>>              OnProductsReady;
    public event Action<Orders>                     OnPurchasesReady;
    public event Action<string, ConfirmedOrder>     OnProductDelivered;
    public event Action<FailedOrder>                OnPurchaseFailedEvent;
    public event Action<bool>                       OnRestoreComplete;

    void Awake() => Instance = this;

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
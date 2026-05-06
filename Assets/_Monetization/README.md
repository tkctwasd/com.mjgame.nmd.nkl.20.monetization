# Monetization Guide

This document explains the Ads and IAP systems under `Assets/_Monetization`.
It describes how each module works, the tracking flow, key terms, and how to use them in your game.

---

## 1. Overview

`Assets/_Monetization` contains two main subsystems:

- `Ads`: AppLovin MAX-based ad modules plus revenue tracking and analytic events
- `IAP`: Unity IAP v5 service integration plus revenue and analytics tracking

These two subsystems are mostly independent:
- Ads does not rely on IAP classes
- IAP does not rely on Ads classes

That means you can use the Ads folder separately from IAP as long as the required shared helpers and SDKs exist in the project.

---

## 2. Ads Architecture

### Core class
- `AdsManager` is the root MonoBehaviour for Ads.
- It initializes AppLovin MAX and creates the submodules:
  - `AppOpenModule`
  - `InterstitialModule`
  - `BannerModule`
  - `MRECModule`
  - `RewardedModule`
- It also holds `AdRevenueTracker` for revenue events.

### Modules

Each ad type has its own module:

- `AppOpenModule`
  - loads and shows App Open ads
  - handles lifecycle events like displayed, hidden, clicked
  - tracks show/click events through `AdAnalytic`

- `InterstitialModule`
  - loads and shows fullscreen interstitial ads
  - obeys remote config capping/delay rules
  - logs show/click events and revenue events

- `BannerModule`
  - creates and shows banner ads
  - logs show and click events

- `RewardedModule`
  - loads and shows rewarded ads
  - logs reward watched and completed events

- `MRECModule`
  - manages medium rectangle ads
  - logs show and click events

### Revenue tracking path

When an ad revenue event occurs, the module calls:

- `OnAdRevenuePaidEvent(...)`
- `AdsManager.SendAdRevenueTracker(impressionData)`
- `AdRevenueTracker.SendAllTracker(adInfo)`

`AdRevenueTracker` sends ad revenue to:

- Firebase Analytics via `FirebaseAnalytics.LogEvent("ad_impression", ...)`
- Adjust via `AdjustSdk.Adjust.TrackAdRevenue(...)` if `USE_ADJUST` is defined
- AppsFlyer via `AppsFlyerSDK.AppsFlyer.logAdRevenue(...)` if `USE_APPSFLYER` is defined

### Analytics events

`AdAnalytic.LogAd(...)` writes Firebase Analytics events like:

- `show_interstitial_ads`
- `show_interstitial_success`
- `show_interstitial_click`
- `show_rewarded_ads`
- `show_rewarded_ads_sucess`
- `show_rewarded_ads_click`
- `show_rewarded_ads_completed`
- `show_banner_ads`
- `show_banner_ads_click`
- `show_app_open_ads`
- `show_app_open_ads_success`
- `show_app_open_ads_click`
- `show_mrec_ads`
- `show_mrec_ads_click`

These events are useful for UA and engagement tracking.

### Important notes for Ads

- Ads require `MaxSdk` / AppLovin MAX.
- Revenue tracking will also require Firebase, AppsFlyer and/or Adjust depending on compile flags.
- If you use only the `Ads` folder in another project, also bring shared helpers such as:
  - `LogMessage.cs`
  - `FirebaseAnalyticManager.cs`
  - `RemoteConfigManager` / config classes used by ad capping
  - `UIManager` if AppOpen resume UI is used

---

## 3. IAP Architecture

### Core classes

- `IAPManager` is the root manager for the IAP system.
- It creates and wires:
  - `IAPService` for Unity Services initialization
  - `IAPCatalogModule` for product catalog building
  - `IAPPurchaseModule` for purchase operations
  - `IAPCallbacks` for store callbacks
  - `IAPRevenueTracker` for revenue event logging

### Service flow

`IAPManager` lifecycle:

1. Awake
   - create callbacks and modules
2. Start
   - call `IAPService.Initialize(...)`
3. OnUnityServicesReady
   - build the catalog
   - connect to store
4. ConnectToStore
   - call `StoreService.Connect()`
   - fetch products via `CatalogProvider.FetchProducts(...)`

### Purchase flow

The current IAP flow is:

1. Player chooses a product
2. `IAPButton.OnClick()` calls `IAPManager.Instance.PurchaseModule.Purchase(ProductId)`
3. `PurchaseModule.Purchase(productId)` verifies readiness and calls `PurchaseService.PurchaseProduct(product)`
4. Store returns callbacks:
   - `OnPurchasePending` when order is pending
   - `OnPurchaseConfirmed` when order is confirmed
   - `OnPurchaseFailed` when purchase fails
5. In `OnPurchasePending`, the system currently logs a purchase attempt event
6. In `OnPurchaseConfirmed`, the system delivers the product and sends revenue tracking
7. `OnPurchaseFailed` logs the failure

### IAP callbacks

`IAPCallbacks` handles store events:

- `OnProductsFetched` → product data loaded
- `OnProductsFetchFailed` → product fetch failure
- `OnPurchasesFetched` → restored purchases loaded
- `OnPurchasesFetchFailed` → restore failure
- `OnPurchasePending` → purchase entered pending state
- `OnPurchaseConfirmed` → purchase confirmed successfully
- `OnPurchaseFailed` → purchase failed
- `OnPurchaseDeferred` → purchase deferred
- `OnStoreDisconnected` → store connection lost

### Purchase module responsibilities

`IAPPurchaseModule` provides:

- `CanPurchase` readiness checks
- `Purchase(string productId)` start purchase
- `Purchase(string productId, Action<string,bool> callback)` convenience callback wrapper
- `FindProduct(productId)` lookup fetched product
- `GetLocalizedPrice(productId)` display price
- `IsPurchased(productId)` check existing confirmed purchase
- `RestorePurchases(callback)` restore purchase flow
- `ConfirmPurchase(pendingOrder)` confirm pending order
- `ConfirmPendingPurchase(productId)` manual pending confirmation

### Revenue + analytics tracking

`IAPRevenueTracker.SendAllTracker(order)` sends IAP revenue on confirmed orders to:

- Firebase via `IAPAnalytic.LogPurchaseSuccess(...)`
- Adjust via `AdjustSdk.Adjust.TrackEvent(...)` with revenue
- AppsFlyer via `AppsFlyerSDK.AppsFlyer.sendEvent(...)` with purchase parameters

`IAPAnalytic` also logs Firebase events for:

- `iap_purchase_attempt`
- `iap_purchase_success`
- `iap_purchase_failed`
- `iap_restore_start`
- `iap_restore_complete`

### Current tracking semantics

- `IAPAnalytic.LogPurchaseAttempt(...)` is triggered in `OnPurchasePending`.
  - This means the store has accepted the transaction and the purchase is in payment flow.
  - It is not exactly the same as "user clicked buy".
- `IAPAnalytic.LogPurchaseSuccess(...)` is sent only after confirmation.
- `IAPAnalytic.LogPurchaseFailure(...)` is sent when the buy fails.

### Recommended improvement for better funnel analysis

If you want to track the exact user intent click:
- add a click event in `IAPButton.OnClick()` or `IAPPurchaseModule.Purchase(productId)`
- use a new event name such as `iap_purchase_clicked` or `iap_purchase_started`

That gives you:
- click started
- purchase pending
- purchase success
- purchase failed

---

## 4. IAP flow diagram

```mermaid
flowchart TD
    UI[User clicks Buy] -->|Purchase()| PurchaseModule
    PurchaseModule -->|PurchaseProduct| StoreService
    StoreService --> OnPurchasePending
    OnPurchasePending -->|log attempt| IAPAnalytic
    OnPurchasePending --> ConfirmPurchase
    ConfirmPurchase -->|confirmed callback| OnPurchaseConfirmed
    OnPurchaseConfirmed -->|send revenue| IAPRevenueTracker
    IAPRevenueTracker --> Firebase[Firebase]
    IAPRevenueTracker --> Adjust[Adjust]
    IAPRevenueTracker --> AppsFlyer[AppsFlyer]
    OnPurchaseConfirmed --> Deliver[Deliver product to game]
    OnPurchaseFailed -->|log fail| IAPAnalytic
```

---

## 5. Key terms

- `PendingOrder`
  - The store has accepted the purchase and is waiting for confirmation.
  - This is typically the stage after the user has started payment and before final confirmation.

- `ConfirmedOrder`
  - The purchase has been completed successfully.
  - This is the moment to deliver the item and count revenue.

- `FailedOrder`
  - The purchase did not complete.
  - The game should handle failure and log it.

- `ad_impression`
  - A Firebase event used for ad revenue tracking.
  - It contains ad metadata such as network, unit, placement, format, country, and revenue value.

- `logAdRevenue`
  - AppsFlyer API call for ad revenue credit.
  - Used when `USE_APPSFLYER` is enabled.

- `TrackAdRevenue`
  - Adjust API call for ad revenue credit.
  - Used when `USE_ADJUST` is enabled.

- `iap_purchase_attempt`
  - A custom Firebase event for the IAP flow starting.
  - In current code it is recorded at pending stage.

- `iap_purchase_success`
  - A custom Firebase event for purchase success.

- `iap_purchase_failed`
  - A custom Firebase event for purchase failure.

---

## 6. How to use

### Ads usage

1. Put `AdsManager` into the scene.
2. Assign `AdConstants` IDs for AppOpen, Interstitial, Banner, MREC, Rewarded.
3. Add `FirebaseAnalyticManager` if you want Firebase analytic logs.
4. Enable compile flags as needed:
   - `USE_APPSFLYER` for AppsFlyer revenue
   - `USE_ADJUST` for Adjust revenue
5. Call ad methods from game code:
   - `AdsManager.Instance.InterstitialModule.ShowInterstitial(location, callback)`
   - `AdsManager.Instance.RewardedModule.ShowRewarded(location, callback)`
   - `AdsManager.Instance.BannerModule.ShowBanner()`
   - `AdsManager.Instance.AppOpenModule.ShowAppOpen(isLoading)`

### IAP usage

1. Put `IAPManager` into the scene.
2. Configure `IAPProducts` or the catalog asset used by the IAP catalog.
3. Add `FirebaseAnalyticManager` if you want analytics.
4. Ensure `USE_IAP` is enabled.
5. Use `IAPButton` or call:
   - `IAPManager.Instance.PurchaseModule.Purchase(productId)`
   - `IAPManager.Instance.PurchaseModule.RestorePurchases(callback)`
6. Listen for purchase results:
   - `IAPManager.Instance.OnProductDelivered`
   - `IAPManager.Instance.OnPurchaseFailedEvent`
   - `IAPManager.Instance.OnPurchasesReady`

### Suggested test plan

- Verify product fetch and UI show correct product names/prices
- Test purchase flow for successful purchase
- Test failed purchase path
- Test restore purchases
- Verify Firebase events are emitted by `FirebaseAnalyticManager`
- Verify revenue events are sent by AppsFlyer / Adjust when enabled

---

## 7. What is ready and what to watch

### Ready for testing
- Ads revenue tracking is wired from MAX revenue paid callbacks to Firebase/Adjust/Appsflyer.
- IAP success revenue tracking is wired from confirmed orders to Firebase/Adjust/Appsflyer.
- Firebase event logging is present for both ad analytics and IAP analytics.

### What to watch
- `IAPAnalytic.LogPurchaseAttempt` currently fires on `OnPurchasePending`, not on immediate click.
- If you want the exact click event, add it in `IAPButton.OnClick()` or `IAPPurchaseModule.Purchase()`.
- Firebase event names are custom and should be agreed with your UA/analytics team.
- Ensure `FirebaseAnalyticManager` is active and ready before IAP/Ad analytic calls.

---

## 8. Recommended next step

If you want, I can also add a small `Monetization/README.md` section showing exact code snippets for:
- `IAPButton` click tracking
- `AdsManager` setup for `AppOpen` / `Interstitial` / `Rewarded`
- `IAP` event names that match UA naming conventions

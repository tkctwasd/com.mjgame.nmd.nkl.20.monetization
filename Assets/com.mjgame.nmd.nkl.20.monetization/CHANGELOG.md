# Changelog

All notable changes to this package will be documented in this file.

---

# [1.0.0] - 2026-10-03

> [!IMPORTANT]
>
> ## First release as a standalone package
>
> Monetization (formerly `Assets/_Monetization`, shipped as `_Monetization_v1.0.0.unitypackage`)
> is now its own Unity package `com.mjgame.nmd.nkl.20.monetization`, built on top of Core `2.x`.

> [!WARNING]
>
> ## Breaking Changes
>
> * Code now compiles into its own assembly and **can no longer see game classes** (`DataSave`, `UIManager` panels...).
> * `DataSave.Instance.DataMonetization` is no longer used. The game must assign `DataMonetization.Current`.
> * Monetization `LogMessage` renamed to `MonetLogMessage` (Core already owns `LogMessage`).
>   Static calls `LogMessage.Ads/IAP/Firebase(...)` must become `MonetLogMessage.Ads/IAP/Firebase(...)`.

### Package

* Added `package.json` (depends on Core `com.mjgame.nmd.nkl.20` 2.0.x, `com.unity.purchasing` 5.4.4).
* Added `Runtime` assembly `com.mjgame.nmd.nkl.20.monetization`.
* Added `Editor` assembly `com.mjgame.nmd.nkl.20.monetization.Editor`.
* Reorganized folders into `Runtime/`, `Editor/`, `Samples~/`.
* Moved `AdsEditor`, `IAPEditor`, `IAPTestEditor` into `Editor/`.
* Added sample `DataSave & Bridge` (`Samples~/DataSaveBridge`).

### Save

* Added `DataMonetization.Current` (same pattern as Core `DataSetting.Current`).
* `AdsManager.IsRemoveAdsPurchased` and `IAPCallbacks` read/write `DataMonetization.Current`.
* Sample `DataSave` now loads `DataMonetization` from PlayerPrefs (previously never loaded).
* Sample `DataSave` bootstraps itself before the first scene so load/save always runs.

### Documentation

* Rewrote `README.md` for the package layout: install, assemblies, defines, Ads, IAP, Firebase, save and troubleshooting.
* Added `CHANGELOG.md`.

### Systems included

* `Ads` - AppLovin MAX: App Open, Interstitial, Rewarded, Banner, MREC + revenue tracking.
* `IAP` - Unity IAP v5: catalog, purchase, restore, revenue tracking (`USE_IAP`).
* `Firebase` - Analytics, Remote Config.
* `Adjust` / `AppsFlyer` - attribution and revenue (`USE_ADJUST`, `USE_APPSFLYER`).
* `Store` - In-App Review.

### Dependencies

* Core `com.mjgame.nmd.nkl.20` - 2.0.x
* `Unity IAP` - 5.4.4
* `TextMeshPro` - 3.0.7
* `Unity UI (uGUI)` - 1.0.0
* `Newtonsoft Json` - 3.2.2
* Imported separately: AppLovin MAX, Firebase, (optional) Adjust, AppsFlyer, Google Play In-App Review.

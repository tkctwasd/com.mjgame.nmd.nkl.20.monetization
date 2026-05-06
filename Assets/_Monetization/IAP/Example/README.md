# IAP Example

This folder contains a sample implementation showing how to use the IAP system in the monetization package.

## Files

- `IAPButtonShopExample.cs` — a clean `IAPButton`-based shop example with three default packs.
- `PanelShop.cs` — a panel-style shop with three `IAPPackButton` items inheriting from `IAPButton`.

## Usage

1. Add `IAPManager` to your scene.
2. Use one of the example components below:
   - `IAPButtonShopExample` for a simple 3-pack button shop.
   - `PanelShop` for a panel-style shop with three `IAPPackButton` items.
3. Create UI buttons in the scene, add the example component, and assign the button references.
4. Optionally assign a `TextMeshProUGUI` `StatusText` to show progress.
5. Open the game and verify the shop loads after `IAPManager` finishes product setup.
6. Click a pack button to start purchase flow.

## IAPButtonShopExample

`IAPButtonShopExample` provides a clean example using `IAPButton` components.
It supports three default demo product packs:

- `com.yourgame.money_small`
- `com.yourgame.remove_ads`
- `com.yourgame.monthly_vip`

These product IDs are also the demo pack IDs defined in `Assets/_Monetization/IAP/Data/IAPProducts.cs`.

Attach `IAPButtonShopExample` to a scene object, assign three `IAPButton` references, and optionally a `StatusText` to show progress.

## PanelShop

`PanelShop` creates a simple shop panel with three `IAPPackButton` items.
Each `IAPPackButton` inherits from `IAPButton`, so it uses the same purchase flow and loading state logic.

Usage:

1. Add `IAPManager` to your scene.
2. Create a new GameObject and attach `PanelShop`.
3. Create three UI buttons and attach `IAPPackButton` to each one.
4. Assign the buttons to `SmallPackButton`, `RemoveAdsPackButton`, and `VipPackButton`.
5. Optionally assign a `StatusText` to show shop progress.
6. Use the default product IDs or override them in the `PanelShop` inspector.

Default `PanelShop` product IDs:

- `com.yourgame.money_small`
- `com.yourgame.remove_ads`
- `com.yourgame.monthly_vip`

`PanelShop` waits for `IAPManager.Instance.IsProductsReady`, subscribes to purchase events, and updates button loading state automatically.

## What it demonstrates

- waiting for `IAPManager.Instance`
- subscribing to `OnProductsReady`
- calling `PurchaseModule.Purchase(productId)`
- handling `OnProductDelivered` and `OnPurchaseFailedEvent`
- displaying product title, price, and status messages

## Notes

- `OnPurchasePending` is handled by the main IAP system, not these examples.
- The example scripts show purchase flow only; add your own game-specific reward delivery inside `OnProductDelivered` events.
- `PanelShop` and `IAPButtonShopExample` both reuse the same IAP purchase logic from `IAPButton`.

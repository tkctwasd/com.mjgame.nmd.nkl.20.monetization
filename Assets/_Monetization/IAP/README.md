# IAP Core — Hướng dẫn tích hợp

Tài liệu này mô tả **phần core của IAP** trong `Assets/_Monetization/IAP` và **cách bạn tự viết code IAP cho game của mình** sau khi bỏ thư mục `Example`.

> Nguyên tắc vàng: **Core không biết gì về kinh tế game của bạn** (Money/Gem/Shield/…).
> Core chỉ lo 3 việc: **RemoveAds, Subscription, và Owned (đã sở hữu)**. Mọi thứ còn lại được
> đẩy ra ngoài qua **một event duy nhất** để game tự xử lý. Nhờ vậy bạn xóa cả `Example`
> đi, core vẫn build & chạy.

---

## 1. Ranh giới Core vs Game

| Thuộc **Core** (giữ nguyên, tái sử dụng mọi game) | Thuộc **Game/Example** (bạn tự viết) |
|---|---|
| `IAPManager` — vòng đời Unity IAP v5, các event store | UI nút mua (kế thừa `IAPButtonUI`) |
| `IAPProducts` — ScriptableObject khai báo sản phẩm | Dữ liệu ví (`DataExample` → của bạn) |
| `IAPPurchaseModule` — mua / restore / query giá | Handler cấp Money/Gem/Shield |
| `MonetizationEntitlements` — RemoveAds / Subscription / Owned + chống cấp trùng | Panel shop, hiển thị số dư |
| `DataMonetization` — trạng thái entitlement đã lưu | |
| `IAPButtonUI` — base class nút mua | |

**Điểm nối duy nhất giữa 2 bên:** event `MonetizationEntitlements.OnGrantReward`.

```
Store confirmed ─► IAPManager.OnProductDelivered
                        │
                        ▼
              MonetizationEntitlements  (CORE)
                        │
        ┌───────────────┼────────────────────────────┐
        ▼               ▼                             ▼
   RemoveAds       Subscription              reward khác (Money/Gem/Shield…)
   (core tự xử lý) (core tự xử lý)                    │
                                                      ▼
                                       OnGrantReward event  ──►  GAME của bạn
```

---

## 2. Luồng mua hàng (đọc 1 lần là hiểu)

1. Người chơi bấm nút → `IAPManager.Instance.PurchaseModule.Purchase(productId)`.
2. Unity IAP xử lý thanh toán → khi xong bắn `OnPurchaseConfirmed`.
3. `IAPManager` gọi `DeliverProduct` → bắn event `OnProductDelivered(productId, ConfirmedOrder)`.
4. `MonetizationEntitlements` (đã tự subscribe) nhận, tra `IAPProducts` để biết product này cho **reward gì**, rồi:
   - `RemoveAds` → ghi entitlement + ẩn ads ngay.
   - `Subscription` → set hạn dùng (Owned + expiry).
   - **reward khác** → bắn `OnGrantReward(reward, expiry)` cho game.
5. Chống cấp trùng bằng `TransactionID` — bạn **không cần** tự lo idempotency.

> `OnPurchasesReady` (khi mở app / restore) cũng chạy đúng luồng Grant này, nên reward được
> khôi phục tự động.

---

## 3. Tự viết code IAP cho game của bạn — 5 bước

### Bước 1 — Bật cờ biên dịch
Thêm `USE_IAP` vào *Scripting Define Symbols* (Project Settings ▸ Player). Không bật thì
`IAPManager` chạy ở chế độ stub (mọi purchase bị bỏ qua), tiện để build khi chưa gắn store.

### Bước 2 — Khai báo sản phẩm trong `IAPProducts`
Tạo asset: **Create ▸ IAP ▸ IAP Products**, rồi điền:

```csharp
new IAPProduct("com.game.remove_ads", ProductType.NonConsumable) { RemoveAds = true }
// Rewards: [ RewardType = RemoveAds ]

new IAPProduct("com.game.gems_100", ProductType.Consumable)
// Rewards: [ RewardType = Gem, Gem = 100 ]
```

Mỗi product có `List<IAPReward> Rewards`. `RewardType` lấy từ enum `IAPRewardType`
(`RemoveAds`, `Subscription` là của core; `Money/Gem/Shield/Brainrot` là ví dụ cho game — xem Bước 5).

### Bước 3 — Viết Reward Handler của riêng bạn
Đây là "bản sao" của `IAPExampleRewardHandler`. Subscribe **một điểm nối duy nhất**:

```csharp
using UnityEngine;

public static class GameRewardHandler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        MonetizationEntitlements.OnGrantReward -= Grant;
        MonetizationEntitlements.OnGrantReward += Grant;
    }

    static void Grant(IAPReward reward, System.DateTime? expiry)
    {
        switch (reward.RewardType)
        {
            case IAPRewardType.Gem:   PlayerWallet.AddGem(reward.Gem);     break;
            case IAPRewardType.Money: PlayerWallet.AddMoney(reward.Money); break;
            // ...thêm reward của game bạn ở đây
        }
        PlayerWallet.Save();
    }
}
```

> **Bạn KHÔNG cần** đụng tới `MonetizationEntitlements`. Chỉ subscribe event.
> RemoveAds/Subscription core đã lo; handler của bạn chỉ xử lý currency của game.

### Bước 4 — Nút mua
Kế thừa `IAPButtonUI` (đã lo loading/enable/restore/refresh giá) hoặc gọi thẳng:

```csharp
IAPManager.Instance.PurchaseModule.Purchase(productId);
// hoặc có callback:
IAPManager.Instance.PurchaseModule.Purchase(productId, (id, ok) => { /* ... */ });
```

Restore: `IAPManager.Instance.PurchaseModule.RestorePurchases(ok => { ... });`

### Bước 5 — (Tùy chọn) Thêm loại reward mới
Nếu game cần loại thưởng chưa có, thêm vào enum `IAPRewardType` trong
[`Data/IAPProducts.cs`](Data/IAPProducts.cs) và thêm field tương ứng trong `IAPReward`,
rồi handle nó trong `GameRewardHandler` (Bước 3). Core **không cần sửa** — reward lạ tự động
rơi vào nhánh `default` và được đẩy ra `OnGrantReward`.

> ⚠️ Enum này là **dữ liệu đã serialize** trong asset `IAPProducts`. Chỉ **thêm** giá trị mới
> ở cuối, đừng xoá/đổi thứ tự giá trị cũ nếu không muốn vỡ asset đang có.

---

## 4. Truy vấn quyền lợi (đọc trạng thái ở bất cứ đâu)

```csharp
MonetizationEntitlements.HasRemoveAds          // đã mua remove ads chưa (kể cả sub còn hạn)
MonetizationEntitlements.IsOwned(productId)    // NonConsumable/Subscription đã sở hữu & còn hạn
MonetizationEntitlements.GetExpiry(productId)  // ngày hết hạn (null = vĩnh viễn)
MonetizationEntitlements.RemoveAdsExpiry       // hạn của remove ads
MonetizationEntitlements.OnChanged             // event: có thay đổi entitlement → refresh UI
```

Ads đã tự đọc `HasRemoveAds` qua `AdsManager.IsRemoveAdsPurchased`, nên mua RemoveAds là ads bị
chặn ở cả Interstitial/AppOpen/Banner/MREC (Rewarded vẫn hiện — cố ý).

---

## 5. Checklist khi bỏ `Example`

Xóa cả thư mục `IAP/Example` → core **vẫn build**. Bạn cần làm lại các thứ sau (vốn nằm trong Example):

- [ ] Viết `GameRewardHandler` thay cho `IAPExampleRewardHandler` (Bước 3).
- [ ] Viết dữ liệu ví của game thay cho `DataExample` (`AddMoney/AddGem/...`).
- [ ] Viết UI shop / nút mua thay cho `IAPPack`, `IAPPackButton`, `IAPButtonRemoveAds`.
- [ ] Cập nhật danh sách sản phẩm trong asset `IAPProducts` cho đúng store của bạn.

Những thứ **không phải làm lại** (core lo hết): purchase/restore, chống cấp trùng, RemoveAds,
Subscription, lưu entitlement, revenue/analytics tracking.

---

## 6. Tham chiếu nhanh API core

| Thành phần | Dùng để |
|---|---|
| `IAPManager.Instance.PurchaseModule.Purchase(id[, cb])` | Bắt đầu mua |
| `IAPManager.Instance.PurchaseModule.RestorePurchases(cb)` | Khôi phục mua |
| `IAPManager.Instance.PurchaseModule.GetLocalizedPrice(id)` | Lấy giá hiển thị |
| `IAPManager.Instance.PurchaseModule.IsPurchased(id)` | Đã mua (theo store) |
| `IAPManager.Instance.OnProductDelivered` | Reward giao thành công (core đã dùng) |
| `IAPManager.Instance.OnPurchaseFailedEvent` | Mua thất bại |
| `MonetizationEntitlements.OnGrantReward` | **Điểm nối game** — cấp currency |
| `MonetizationEntitlements.HasRemoveAds / IsOwned / GetExpiry` | Truy vấn quyền lợi |

Bản mẫu hoàn chỉnh làm đúng theo tài liệu này: xem **[`Example/README.md`](Example/README.md)**.

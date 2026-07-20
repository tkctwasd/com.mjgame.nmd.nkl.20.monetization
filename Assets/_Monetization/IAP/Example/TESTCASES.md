# IAP Test Packs

Điền bằng `Tools > IAP > Fill Test Packs`. Sau đó tạo đúng SKU trên Play Console / App Store Connect.

Quy ước: `,` = pack riêng · `+` = pack gộp.

---

## 1. Ma trận pack

| # | ProductId | Type | Reward | Mục đích |
|---|---|---|---|---|
| 1 | `test.con_gem10` | Consumable | 10 gem | Baseline, mua lặp |
| 2 | `test.con_coin10` | Consumable | 10 coin | Tiền tệ thứ hai |
| 3 | `test.non_gem10` | NonConsumable | 10 gem | ⚠️ Bẫy cấp trùng |
| 4 | `test.non_removeads` | NonConsumable | remove ads | Entitlement vĩnh viễn |
| 5 | `test.non_gem100_removeads` | NonConsumable | 100 gem + remove ads | Mẫu pack combo |
| 6 | `test.sub_gem100` | Subscription 2d | 100 gem | Gia hạn có cấp lại? |
| 7 | `test.sub_shield` | Subscription 2d | shield | Hết hạn phải thu hồi |
| 8 | `test.sub_removeads` | Subscription 2d | remove ads | ⚠️ Bẫy hết hạn |

---

## 2. Kết quả mong đợi

### Consumable (1, 2)
- Mua lần 1 → cộng đúng số lượng
- Mua lần 2 liên tiếp → **cộng dồn**, không bị chặn "already owned"
- Restore → **không** cấp lại
- Kill app giữa lúc pending → mở lại phải tự cấp nốt (pending order được confirm)

### NonConsumable (3, 4, 5)
- Mua lần 1 → cấp thưởng, nút chuyển "Owned", disable
- Mua lại → store báo "already owned", **không charge lần 2**
- Cài lại app → Restore → quyền lợi trở lại, **gem KHÔNG được cộng lần nữa** (case 3, 5)
- Case 4, 5 → banner/inter/AOA/MREC tắt; **rewarded vẫn xem được**
- Offline sau khi mua → vẫn phải nhận là đã mua

### Subscription (6, 7, 8)
- Mua → cấp quyền lợi, nút hiện "Subscribed"
- Còn hạn → `IsPurchased` = true
- **Hết hạn → quyền lợi bị thu hồi**; case 8 ads phải hiện lại
- Gia hạn → chốt rõ có cấp lại 100 gem mỗi kỳ hay chỉ 1 lần (case 6)
- Huỷ giữa kỳ → vẫn dùng tới hết hạn đã trả

---

## 3. Ba lỗ hổng bộ test này sẽ phơi ra

Hiện tại `DataMonetization` chỉ có đúng một field `IsRemoveAdsPurchased` — chưa đủ để pass các case dưới.

### 3.1 — Case 8 sẽ FAIL: sub remove-ads không bao giờ hết hạn
`IAPButtonRemoveAds.SaveRemoveAdsPurchased()` ghi cờ bool **vĩnh viễn**. Sub hết hạn sau 2 ngày nhưng cờ vẫn `true` → user hết hạn vẫn không thấy ads, mất doanh thu IAA.

Cần `DateTime RemoveAdsExpiry` (giá trị `MaxValue` cho NonConsumable, ngày hết hạn cho Subscription), và `IsRemoveAdsPurchased` so với `DateTime.UtcNow`.

### 3.2 — Case 3, 5 sẽ FAIL: restore cấp trùng gem
NonConsumable restore được vô hạn lần. Không có sổ ghi "transaction nào đã cấp thưởng" thì mỗi lần restore lại cộng thêm 10/100 gem → user farm gem miễn phí.

Cần `List<string> GrantedTransactionIds` trong `DataMonetization`, check trước khi cấp.

### 3.3 — Case 5 sẽ FAIL: pack combo không cấp gì cả
Đường ghi entitlement duy nhất hiện nay nằm trong `IAPButtonRemoveAds.OnPurchaseSuccess`. Mua pack combo qua `IAPPackButton` → không ai ghi cờ, không ai cộng gem. `GetRewardsByProductId` vẫn chưa có caller.

Cần listener cấp manager nghe `OnProductDelivered` + `OnPurchasesReady`, duyệt `Rewards`, cấp thưởng theo `RewardType`.

---

## 4. Lưu ý khi dựng SKU trên store

- **Không store nào có gói 2 ngày.** Play và App Store tối thiểu là 1 tuần. Tạo SKU **weekly**, và dùng `SubscriptionDays = 2` như thời hạn entitlement *phía client* để test logic hết hạn.
- Test hết hạn nhanh: Google Play license tester gói weekly gia hạn ~5 phút; Apple Sandbox 1 tuần ≈ 3 phút.
- Sub trên iOS phải cùng một Subscription Group thì mới test được nâng/hạ cấp.
- Sau khi đổi SKU nhớ chờ Play Console propagate (vài chục phút) trước khi build test.

---

## 5. Case chưa có trong bộ 8 — cân nhắc thêm

| Case | Vì sao cần |
|---|---|
| ProductId khai trong SO nhưng chưa có trên store | Kiểm tra UI fallback, không crash |
| User bấm mua rồi bấm Cancel | `OnPurchaseFailed` → `PanelOverlay` phải tắt, nút mở lại |
| Deferred / Ask to Buy (iOS) | Game không được kẹt ở overlay chờ |
| Mất mạng giữa lúc mua | Pending order phải confirm lại ở lần mở sau |
| Restore khi chưa mua gì | `OnRestoreComplete(true)` nhưng không cấp gì, cần thông báo đúng |
| Đổi tài khoản store giữa chừng | Entitlement cũ phải bị thu hồi |

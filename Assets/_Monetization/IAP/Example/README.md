# IAP Example — Bản mẫu tham chiếu

Thư mục này là **một bản triển khai hoàn chỉnh làm ĐÚNG theo** [`../README.md`](../README.md)
(hướng dẫn IAP Core). Mục đích: cho bạn xem "một game thật cắm vào core như thế nào", rồi
**xoá cả thư mục này** và viết bản của riêng bạn theo đúng khuôn mẫu.

> Toàn bộ code ở đây phụ thuộc **một chiều** vào core (`Example → Core`). Core **không** biết
> Example tồn tại. Vì vậy xoá thư mục này đi core vẫn build & chạy bình thường.

---

## Mỗi file map với bước nào trong hướng dẫn Core

| File Example | Làm đúng theo | Vai trò |
|---|---|---|
| [`IAPExampleRewardHandler.cs`](IAPExampleRewardHandler.cs) | **Bước 3** — Reward Handler | Điểm nối duy nhất: subscribe `MonetizationEntitlements.OnGrantReward`, cấp Money/Gem/Shield vào ví. **Đây là file quan trọng nhất để bắt chước.** |
| [`IAPDataExample.cs`](IAPDataExample.cs) | **Bước 3** — dữ liệu ví | `partial class DataExample` chứa `Money/Gem/Shield` + `AddMoney/AddGem/AddShield`. Bản game của bạn thay bằng ví riêng. |
| [`IAPButtonRemoveAds.cs`](IAPButtonRemoveAds.cs) | **Bước 4** — nút mua | Kế thừa `IAPButtonUI`, chỉ lo hiển thị "Owned". Việc ghi entitlement + ẩn ads do core lo. |
| [`IAPPackButton.cs`](IAPPackButton.cs) | **Bước 4** — nút mua | Nút mua gói (pack) nhiều reward, hiển thị nội dung gói. |
| [`IAPPack.cs`](IAPPack.cs) | Query + UI | Panel shop: đọc số dư qua `IAPExampleRewardHandler.Money/Gem/Shield` và `MonetizationEntitlements.HasRemoveAds`; refresh khi `OnChanged`. |
| [`TESTCASES.md`](TESTCASES.md) | Kiểm thử | Kịch bản test tay cho luồng mua/restore/entitlement. |

---

## Điều Example CỐ Ý không làm (để bạn thấy ranh giới)

- **Không** tự cộng tiền bên trong `MonetizationEntitlements`. Currency chỉ được cấp trong
  `IAPExampleRewardHandler` — qua event. Đây chính là thứ giữ cho core sạch.
- **Không** tự chống cấp trùng. Core đã khử trùng bằng `TransactionID` trước khi bắn event,
  nên handler cứ cộng thẳng.
- **Không** đụng RemoveAds / Subscription. Hai loại này core xử lý trọn; Example chỉ *hiển thị*
  trạng thái (`HasRemoveAds`).

---

## Cách dùng Example như bàn đạp

1. Chạy thử scene shop mẫu, mua vài sản phẩm, xem `IAPExampleRewardHandler` cộng số dư và
   `MonetizationEntitlements` chặn ads.
2. Đối chiếu từng file ở bảng trên với bước tương ứng trong [`../README.md`](../README.md).
3. Khi đã hiểu, **xoá thư mục `Example`** rồi làm lại 4 việc trong *Checklist khi bỏ Example*
   (mục 5 của hướng dẫn Core) bằng code của game bạn.

> Nếu sau khi xoá mà thấy lỗi build, gần như chắc chắn do còn chỗ trong game gọi tới
> `IAPExampleRewardHandler` hoặc `DataExample`. Thay chúng bằng ví/handler của bạn là xong —
> **không** cần sửa file nào trong core.

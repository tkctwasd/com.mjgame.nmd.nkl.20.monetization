# Monetization — Cài đặt nhanh cho project mới

Thư mục `_Monetization` được thiết kế để **bê nguyên** sang project Unity khác
(giữ kiến trúc no-asmdef, code compile chung vào `Assembly-CSharp`).
Tài liệu này là các bước để chạy được sau khi clone/submodule.

> Yêu cầu: **Unity 2022.3.x** (bản gốc: 2022.3.62f3).

---

## 0. Đưa thư mục vào project

Chọn 1 trong các cách:

**Cách A — Git Release + `.unitypackage`** (khuyến nghị, nhanh nhất cho người dùng):
1. Tạo file: hoặc bấm tay `Tools > Monetization > Export .unitypackage`, hoặc để **CI tự làm** (xem mục 5) —
   chỉ cần `git tag v1.0.0 && git push origin v1.0.0`.
2. Đồng nghiệp: vào trang **Releases** của repo, tải `_Monetization.unitypackage`, double-click import vào Unity.
3. Update: tải release mới, import đè.

**Cách B — git submodule** (version theo commit, cần biết git):
```bash
git submodule add <URL_REPO_MONETIZATION> Assets/_Monetization
git submodule update --init --recursive
```

**Cách C — copy tay:** copy cả thư mục `_Monetization` (kèm file `.meta`) vào `Assets/`.

> Cả 3 cách đều giữ kiến trúc no-asmdef; các bước cài dependency bên dưới là như nhau.

---

## 1. Chạy Setup tool (làm hộ 90% việc)

Trong Unity mở menu: **`Tools > Monetization > Setup`**

Cửa sổ Setup có 3 việc, làm theo thứ tự:

1. **① Thêm scoped registry + UPM deps** — tự ghi vào `Packages/manifest.json`:
   - scoped registry: *AppLovin MAX*, *OpenUPM* (external-dependency-manager)
   - UPM packages: `com.unity.nuget.newtonsoft-json`, `com.unity.purchasing`, `com.applovin.mediation.ads`
   - (có tạo `manifest.json.bak` để backup)
   - Đợi Unity resolve/import xong.

2. **② Import SDK còn thiếu** — bảng trạng thái đánh dấu `✘` cho SDK chưa có, bấm **Mở link** để tải:
   - **Firebase** (Analytics + RemoteConfig) — *bắt buộc*
   - **Adjust** — *tùy chọn*
   - **AppsFlyer** — *tùy chọn*

3. **③ Auto set scripting defines** — chỉ bật define cho SDK **đã có mặt** (tránh vỡ compile):
   - `USE_IAP` (khi có Unity Purchasing)
   - `USE_ADJUST` (khi có Adjust)
   - `USE_APPSFLYER` (khi có AppsFlyer)

> Bảng trạng thái: `✔` = đã có, `✘` = còn thiếu; `[BẮT BUỘC]` phải có mới compile được,
> `[tùy chọn]` thiếu vẫn compile (code được bọc trong `#if`).

---

## 2. Dependency — tổng hợp

| SDK | Bắt buộc? | Define | Cài bằng |
|---|---|---|---|
| AppLovin MAX | ✅ | — | UPM (`com.applovin.mediation.ads`) sau khi có scoped registry |
| Firebase Analytics | ✅ | — | Import `.unitypackage` từ Firebase |
| Firebase RemoteConfig | ✅ | — | Import `.unitypackage` từ Firebase |
| Firebase Crashlytics | ✅ | — | Import `.unitypackage` từ Firebase |
| Newtonsoft Json | ✅ | — | UPM (`com.unity.nuget.newtonsoft-json`) |
| Unity IAP | ⛅ tùy chọn | `USE_IAP` | UPM (`com.unity.purchasing`) |
| Adjust | ⛅ tùy chọn | `USE_ADJUST` | Tải & import tự động (Setup tool) hoặc GitHub Adjust |
| AppsFlyer | ⛅ tùy chọn | `USE_APPSFLYER` | Tải & import tự động (Setup tool) hoặc GitHub AppsFlyer |

> **Firebase/Adjust/AppsFlyer không phải UPM** nên Setup tool chỉ đưa link, phải import tay.
> Nhớ thêm `google-services.json` (Android) / `GoogleService-Info.plist` (iOS) cho Firebase,
> và cấu hình MAX SDK Key + Ad Unit ID, Adjust/AppsFlyer app token.

---

## 3. Dùng trong game

Xem chi tiết kiến trúc và cách gọi trong:
- [`README.md`](README.md) — tổng quan Ads + IAP.
- [`IAP/README.md`](IAP/README.md) — hợp đồng tích hợp IAP + 5 bước tự code.
- [`IAP/Example/`](IAP/Example/) — scene + prefab mẫu.

Tóm tắt: thả `AdsManager` và `IAPManager` vào scene khởi động, điền config (Ad Unit ID,
IAP product id), gọi API tương ứng. Mua RemoveAds qua IAP sẽ tự chặn Ads qua
`MonetizationEntitlements`.

---

## 4. Xử lý lỗi thường gặp

- **Vỡ compile ngay sau khi import**: thường do define bật cho SDK chưa cài. Mở
  `Tools > Monetization > Setup` → bấm **③ Auto set scripting defines** để đồng bộ lại.
- **Không thấy package AppLovin**: chưa thêm scoped registry — chạy lại bước **①**, hoặc
  kiểm tra `Packages/manifest.json` có registry `unity.packages.applovin.com`.
- **Firebase lỗi `namespace not found`**: chưa import `.unitypackage` Firebase, hoặc thiếu
  External Dependency Manager (EDM4U) — có sẵn qua scoped registry OpenUPM.

---

## 5. Ra bản phát hành (export tay + CI tạo Release)

Cách này export tay (0 phút Unity CI) và để CI nhẹ tự tạo Release — gần như không tốn compute minutes.

**Bước 1 — trong Unity:** mở `Tools > Monetization > Release (Export + Changelog)`
→ nhập **Version** + **Changelog** → bấm **Export & Stage**. Sinh ra trong `Assets/Release/`:
- `_Monetization_v<version>.unitypackage`
- `version.txt`, `CHANGELOG_LATEST.md`, và cộng dồn `CHANGELOG.md`

**Bước 2 — commit & push** (lên nhánh mặc định):
```bash
git add Assets/Release
git commit -m "release: v1.0.1"
git push
```

**Bước 3 — CI tự chạy:** [`.gitlab-ci.yml`](../../.gitlab-ci.yml) phát hiện `version.txt` thay đổi →
tạo tag `v1.0.1` + **GitLab Release**, description = changelog, đính link tải file `.unitypackage`.

> - Không cần Unity trong CI, không cần secret (dùng sẵn `CI_JOB_TOKEN`).
> - Mỗi bản **phải bump version** (release trùng tag sẽ fail — đúng ý đồ).
> - Chỉ chạy trên **nhánh mặc định**; đang ở nhánh khác thì merge vào main rồi push.
> - Repo private → link tải yêu cầu người tải có quyền truy cập repo.

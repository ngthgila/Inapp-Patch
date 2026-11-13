# Ghi chú cơ chế đã hoàn thiện

Tài liệu này lưu lại cách triển khai đã hoàn thành cho Picsart và Grammarly để tiện tra cứu, bảo trì sau này.

### 1. Ứng dụng Picsart (`com.picsart.studio`)

#### 🎯 Mục tiêu

- Ép hiển thị bảng giá theo mong muốn: Gói Pro 1 Tháng (500 Credit), Gói Plus 1 Tháng và Gói Ultra 1 Tháng (1500 Credit), thay vì bị dính A/B Test ép mua gói Tuần (`pro_weekly`) hoặc gói năm.
- Sửa lỗi Google Play: `Error: We are unable to change your subscription plan. Manage subscriptions.`
- Reset định danh thiết bị Picsart để mua liên tục nhiều tài khoản mà giữ nguyên tài khoản Google Play, không làm mất nick Gmail trên máy.

#### ⚙️ Cơ chế và kỹ thuật can thiệp

##### 1. Bảng giá Paywall (`settings.json` khoảng 2.5 MB)

- Picsart tải cấu hình dynamic paywall về và lưu tại:

  ```text
  /data/data/com.picsart.studio/files/settings.json
  ```

- Tool sử dụng lệnh `sed` dạng chuỗi tĩnh tối ưu, không dùng capture group `\(..\)` để tránh timeout trên Android, nhằm thay thế hàng loạt:
  - Chuyển `pro_weekly` / `pro_weekly_100` thành `pro_monthly`, hoặc `ultra_monthly` với cờ `"is_selected":true`.
  - Ép `package_sku` sang đúng gói mong muốn:
    - Pro 1 Tháng chuẩn: `subscription_pro_monthly` — nhận trọn 500 credit/tháng thay vì bản 100 credit.
    - Ultra 1 Tháng: `subscription_ultra_monthly` — 1500 credit/tháng.
  - Đổi tiêu đề UI `title_12` / `subtitle_8` và `content_id` (`pro_tab_default` / `plus_tab_default`).

##### 2. Fix lỗi “Unable to change your subscription plan”

- Lỗi xảy ra do Picsart lưu cache token thuê bao cũ và định danh phần cứng trong các file:

  ```text
  /data/data/com.picsart.studio/files/device-id
  /data/data/com.picsart.studio/files/internal-device-id
  /data/data/com.picsart.studio/shared_prefs/subscription.shared.preferences.xml
  /data/data/com.picsart.studio/shared_prefs/com.facebook.internal.iap.IAP_CACHE_GPBLV2V7.xml
  ```

- `subscription.shared.preferences.xml` chứa các khóa như `store_subscription_validation_key` và `guard_subscription_key`.
- Cách xử lý:
  - Xóa toàn bộ các file cache liên quan.
  - Random một mã `device_id` mới gồm 32 ký tự hex, có dạng `d_xxxxxxxx...`.
  - Ghi đè mã mới vào `com.picsart.analytics.xml`.
- Nhờ đó, Google Play nhận diện đây là một phiên mua mới từ đầu và không bị vướng trạng thái đổi gói (`change plan`).

##### 3. Tích hợp vào GUI

Đã thêm các nút trong `PicsartToolGUI.cs` và biên dịch thành `PicsartToolGUI.exe`:

- 🔥 Picsart: Ép Gói Ultra (1 Tháng - 1500 Credit)
- 💎 Picsart: Ép Gói Pro (1 Tháng - 500 Credit)
- ⭐ Picsart: Ép Gói Plus (1 Tháng)
- 🧹 Reset Dữ Liệu Picsart (Giữ Gmail)

---

### 2. Ứng dụng Grammarly (`com.grammarly.android.keyboard`)

#### 🎯 Mục tiêu

- Bỏ qua giai đoạn dùng thử 7 ngày (Free Trial), để khi nhấn đăng ký sẽ chuyển thẳng sang màn hình thanh toán tiền ngay lập tức: `Continue with Plan` thay vì `Start Free Trial`.

#### ⚙️ Cơ chế và kỹ thuật can thiệp

##### 1. Hệ thống cấu hình thử nghiệm A/B (Manakin Treatments)

- Grammarly quản lý các biến thể người dùng (Treatment) bằng Protobuf tại:

  ```text
  /data/data/com.grammarly.android.keyboard/files/datastore/manakin-treatments
  ```

- Cờ điều khiển dùng thử: `android_free_trial`.
  - Nhóm có dùng thử: `test_1` — hex `2A06746573745F31`.
  - Nhóm mua thẳng, không dùng thử: `control_1` — hex `2A09636F6E74726F6C5F31`.

##### 2. Cách can thiệp

- Kéo file `manakin-treatments` ra máy tính.
- Patch trực tiếp block `android_free_trial` từ `test_1` sang `control_1`.
- Chỉnh lại độ dài trường Protobuf tương ứng từ `1A66` thành `1A69`.
- Đẩy file trở lại điện thoại.
- Gán lại quyền sở hữu UID cho ứng dụng.
- Đặt quyền `chmod 440` (chỉ đọc) để ứng dụng không thể tự đồng bộ lại cờ dùng thử từ máy chủ.

##### 3. Tích hợp vào GUI

- Nút Grammarly tắt dùng thử/mua thẳng đã được tích hợp trong `PicsartToolGUI.exe`.

---

### 3. Ứng dụng Chess.com (`com.chess`)

Cơ chế Chess.com được ghi riêng vì cần mô tả chi tiết luồng Google Play Billing, cấu trúc cache, điều kiện xác minh và rollback:

- [CHESS_NO_TRIAL.md](CHESS_NO_TRIAL.md)


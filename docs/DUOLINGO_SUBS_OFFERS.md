# Phân tích subscription và offer Duolingo

Ngày kiểm tra: 2026-11-13  
Thiết bị: `21f11460800c7ece`  
Package: `com.duolingo`  
Phiên bản: `6.105.2` (`versionCode=2497`)

## Kết luận nhanh

Google Play đã trả đầy đủ SKU Duolingo Max cho thiết bị và tài khoản Play hiện tại. Tuy nhiên paywall `COMPARE PLANS` của tài khoản Duolingo đang đăng nhập chỉ hiển thị `Super` và `Super Family`, không hiển thị Max.

Vì vậy lớp chặn hiện tại không nằm ở Google Play Billing. Duolingo lọc Max ở tầng paywall/eligibility theo tài khoản hoặc A/B test trước khi cho người dùng chọn SKU.

Các chuỗi điều kiện tìm thấy trong DEX bản hiện tại gồm:

- `shouldShowMaxPurchaseFlow`
- `SubscriptionTab_MaxCardEligible`
- `SubscriptionTab_MaxFamilyCardEligible`
- `max_package_page`
- `max_view_all_plans_purchase_page`
- `max_nav_bar_package_card`
- `combined_package_page_max_individual_card`
- `combined_package_page_max_family_card`

Các cờ này cho thấy client có lớp eligibility riêng để quyết định có dựng card/luồng mua Max hay không. Giá và offer từ Play chỉ được dùng sau khi card Max đã được chọn cho tài khoản.

## Tổng quan cache Billing

- 23 subscription product ID khác nhau.
- 51 pricing phase sau khi tách base plan và các giai đoạn trial.
- Tiền tệ hiện tại: VND.
- Tất cả offer Duolingo quan sát được có `offerTags=[]`.
- Cache Max được xác nhận còn hiệu lực tại thời điểm quét.

## Toàn bộ subscription product

| Nhóm | Product ID | Plan | Offer | Giá/giai đoạn |
|---|---|---|---|---|
| Premium cũ | `com.duolingo.subscription.premium.sixmonth.roundedmonthly.20191112.4799` | 6 tháng | base | ₫1.080.000 / 6 tháng |
| Premium cũ | `com.duolingo.subscription.premium.trial14.sixmonth.roundedmonthly.20200615.4799` | 6 tháng | base + `freetrial` | 2 tuần miễn phí → ₫1.080.000 / 6 tháng |
| Premium cũ | `com.duolingo.subscription.premium.trial7.onemonth.20191120.1299` | 1 tháng | base + `freetrial` | 1 tuần miễn phí → ₫99.000 / tháng |
| Premium cũ | `com.duolingo.subscription.premium.trial7.sixmonth.roundedmonthly.20190614.4799` | 6 tháng | base + `freetrial` | 1 tuần miễn phí → ₫1.080.000 / 6 tháng |
| Premium cũ | `com.duolingo.subscription.premium.trial7.twelvemonth.roundeddiscount.20190614.8399` | 1 năm | base + `freetrial` | 1 tuần miễn phí → ₫599.000 / năm |
| Lite | `lite.t7.12m.26q2litertr.3599` | 1 năm | base + `freetrial` | 1 tuần miễn phí → ₫259.000 / năm |
| Max | `max.12m.2024q3max.16799` | 1 năm | base | ₫1.199.000 / năm |
| Max | `max.1m.2024q3max.2999` | 1 tháng | base | ₫199.000 / tháng |
| Max Family (suy ra từ `fp`) | `max.fp.12m.2024q3max.23999` | 1 năm | base | ₫1.799.000 / năm |
| Max | `max.t14.12m.2024q3max.16799` | 1 năm | base + `freetrial` | 2 tuần miễn phí → ₫1.199.000 / năm |
| Max | `max.t14.1m.2024q3max.2999` | 1 tháng | base + `freetrial` | 1 tuần miễn phí → ₫199.000 / tháng |
| Max Family (suy ra từ `fp`) | `max.t14fp.12m.2024q3max.23999` | 1 năm | base + `freetrial` | 2 tuần miễn phí → ₫1.799.000 / năm |
| Super | `super.12m.2024q3max.8399` | 1 năm | base | ₫599.000 / năm |
| Super | `super.12m.nyp2027.5999` | 1 năm | base | ₫475.000 / năm |
| Super | `super.1m.2024q3max.1299` | 1 tháng | base | ₫99.000 / tháng |
| Super | `super.1m.nyp2027.1699` | 1 tháng | base | ₫132.000 / tháng |
| Super Family (suy ra từ `fp`) | `super.fp.12m.2024q3max.11999` | 1 năm | base | ₫899.000 / năm |
| Super | `super.t14.12m.2024q3max.8399` | 1 năm | base + `freetrial` | 2 tuần miễn phí → ₫599.000 / năm |
| Super | `super.t14.1m.2024q3max.1299` | 1 tháng | base + `freetrial` | 1 tuần miễn phí → ₫99.000 / tháng |
| Super Family (suy ra từ `fp`) | `super.t14fp.12m.2024q3max.11999` | 1 năm | base + `freetrial` | 2 tuần miễn phí → ₫899.000 / năm |
| Super | `super.t7.12m.26q2litertr.11999` | 1 năm | base + `freetrial` + `1m-extended-ft` | base ₫860.000; trial 1 tuần hoặc 1 tháng → ₫860.000 / năm |
| Super Family (suy ra từ `fp`) | `super.t7fp.12m.26q3fp.16799` | 1 năm | base + `freetrial` | 1 tuần miễn phí → ₫1.259.000 / năm |
| Super | `super.t7noft.1m.26q2litertr.1699` | 1 tháng | base | ₫132.000 / tháng |

## Chi tiết riêng của Max

Google Play trả sáu biến thể Max, bao phủ:

- Max cá nhân tháng và năm, mua thẳng.
- Max Family năm, mua thẳng.
- Max cá nhân tháng và năm có trial.
- Max Family năm có trial.

Lưu ý: product ID `max.t14.1m.2024q3max.2999` có chữ `t14`, nhưng pricing phase thực tế Google Play trả về là `P1W` (1 tuần), không phải 14 ngày. Hai gói năm `max.t14.*12m*` trả đúng `P2W`.

## Dấu vết A/B và eligibility

Các file cục bộ đã kiểm tra:

- `ab_prefs.xml`
- `ExperimentsAttemptedTreatmentsPrefs.xml`
- `early_lifecycle_experiments.xml`
- `PlusPrefs:<user>.xml`
- `user_<user>_subscription_tab.xml`
- `user_<user>_package_preference_model.xml`

Không có treatment Max dạng rõ trong các XML này. `package_preference_model` có timestamp tải nhưng giá trị model/percentile hiện trống. Khả năng cao quyết định `MaxCardEligible` đến từ response tài khoản/shop của server và chỉ tồn tại trong model runtime hoặc kho dữ liệu không lưu dưới dạng XML dễ đọc.

## Trình tự quyết định suy ra

```text
Tài khoản Duolingo / server experiment
        ↓
MaxCardEligible + shouldShowMaxPurchaseFlow
        ↓
Client dựng card Max trên paywall
        ↓
Client chọn product ID Max phù hợp
        ↓
Google Play ProductDetails / offer token
        ↓
Màn hình thanh toán
```

Thiết bị hiện dừng ở bước thứ nhất/thứ hai: ProductDetails Max đã có, nhưng UI không dựng card Max.

## Phạm vi an toàn

Quá trình kiểm tra chỉ đọc database/cache và mở paywall hiện tại. Không sửa treatment, không sửa ProductDetails, không chọn offer token và không khởi tạo giao dịch.

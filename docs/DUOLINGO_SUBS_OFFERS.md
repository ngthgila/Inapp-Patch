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

Giai đoạn quét ban đầu chỉ đọc database/cache và mở paywall hiện tại. Không sửa ProductDetails, không chọn offer token và không khởi tạo giao dịch.

## Kiểm chứng route Max ngày 2026-11-13

### Max cá nhân

Duolingo khai báo deep link chính thức `duolingo://max`. Lệnh sau mở được màn hình giới thiệu Max ngay cả khi card Max đang bị ẩn trong tab Subscription:

```powershell
adb -s 21f11460800c7ece shell am start -W -a android.intent.action.VIEW -d "duolingo://max" com.duolingo
```

Màn hình đã kiểm chứng có tiêu đề `Duolingo Max is the best plan for advanced learning` và CTA thử miễn phí. Đây là route chính thức, không cần sửa APK hay giả trạng thái thuê bao.

### Max Family 1 năm

DEX có đầy đủ các context dành riêng cho Max Family:

- `COMBINED_PACKAGE_PAGE_MAX_FAMILY_CARD`
- `CHOOSE_A_PLAN_SETTING_MAX_FP_CARD`
- `MAX_FAMILY_SUBSCRIPTION_UPSELL_TAB_HEADER`
- `IMMERSIVE_MAX_FP_OFFER_ONBOARDING_ORIGIN`
- `IMMERSIVE_MAX_FAMILY_PLAN_FOR_SECONDARY_SHOP`

Activity `ImmersiveMaxFamilyPlanOwnerOnboardingActivity` cũng dựng được giao diện ghi rõ `Max familiar`. Tuy nhiên không được gọi trực tiếp activity này để thử UI: kiểm tra ngày 2026-11-13 xác nhận nó có side effect tạo một preview Max Family 72 giờ trên server Duolingo, không chỉ tạo trạng thái local.

Khi gọi trực tiếp `PlusPurchaseFlowActivity` với context `COMBINED_PACKAGE_PAGE_MAX_FAMILY_CARD`, app ban đầu vẫn dựng nội dung Max nhưng trước bước Google Play lại chuyển sang CTA `Get Super`. Context `IMMERSIVE_MAX_FP_OFFER_ONBOARDING_ORIGIN` cho kết quả tương tự. Không có Billing flow Max Family nào được khởi tạo.

Kết luận: Max Family năm tồn tại thật trong Google Play (`max.fp.12m.2024q3max.23999` và bản trial `max.t14fp.12m.2024q3max.23999`), nhưng chỉ có ProductDetails trong cache Google Play là chưa đủ. Server Duolingo phải trả một package `type: "gold"` trong `subscription_catalog.json`; nếu catalog chỉ có `premium`/`lite`, luồng Max sẽ bị hạ về Super trước checkout.

### Cờ runtime đã xác định

`PlusContext.isFromMaxFPPromoHook()` trả true cho sáu context sau:

- `COMBINED_PACKAGE_PAGE_MAX_FAMILY_CARD`
- `COMBINED_PACKAGE_PAGE_MAX_FAMILY_CARD_UPGRADE`
- `MAX_FP_UPGRADE_NUDGE_NOTIFICATION`
- `MAX_FP_UPGRADE_NUDGE_DRAWER`
- `CHOOSE_A_PLAN_SETTING_MAX_FP_CARD`
- `MAX_FAMILY_SUBSCRIPTION_UPSELL_TAB_HEADER`

Luồng eligibility của tab Subscription được tạo từ `SubscriptionTab_MaxCardEligible` và `SubscriptionTab_MaxFamilyCardEligible`. Trong bản `6.105.2`, Max Family còn phụ thuộc đồng thời vào server flags, `userEligibleForMaxCards`, trạng thái thành viên Family, các hook Max-to-Max-Family và `shouldShowRetieredViews`.

### Giới hạn an toàn của thử nghiệm

Các hook runtime chỉ được dùng tạm thời để quan sát nhánh UI và đã bị gỡ bằng cách dừng tiến trình Duolingo. Không ghi đè APK, không thay ProductDetails/offer token và không xác nhận giao dịch Google Play. Nút Max Family không được thêm vào GUI vì route hiện tại chưa đảm bảo chọn đúng SKU Family; tránh hiển thị một nút có thể âm thầm rơi về Super.

## Sự cố preview Max Family 72 giờ ngày 2026-11-13

### Trạng thái server đã xác nhận

Sau khi activity onboarding nội bộ được gọi trực tiếp, response user của Duolingo xuất hiện:

- `hasPlus: true`
- `productId: com.duolingo.immersive_gold_free_trial_family_subscription`
- `renewer: IMMERSIVE_PLUS`
- `itemType: immersive_gold_subscription`
- Family owner là chính tài khoản đang đăng nhập
- Thời hạn đúng 72 giờ: từ `2026-11-13 15:35:12` đến `2026-11-16 15:35:12` (Asia/Bangkok)

Không ghi vào tài liệu public các token mời, vendor purchase ID hoặc ID tài khoản lấy từ response.

File DataStore local ban đầu có cờ `immersive_max_family_plan_*`; xóa file này không hủy trạng thái vì server sẽ đồng bộ lại. Sau khi đồng bộ, app tạo lại trạng thái family plan và response user vẫn báo gói immersive Gold đang hoạt động. Vì vậy đây không phải lỗi cache có thể sửa bằng cách xóa dữ liệu local.

### Vì sao Max hiện nhưng không mua được

Các catalog của tài khoản tại thời điểm kiểm tra nằm ở:

```text
/data/data/com.duolingo/files/res/v2/subscription/<user_id>/<country>/<course>/subscription_catalog.json
```

Tất cả catalog đã kiểm tra cho `VN`, `XX`, `IN` và `LK` chỉ trả các package `type: "premium"` (Super) và `type: "lite"`; không có package `type: "gold"` (Max). Trong khi đó response user lại báo đang có preview `immersive_gold_subscription`. Kết quả thực tế:

```text
Max intro → Get Duolingo Max → Max feature list → Join Super today
```

Google Play Billing chưa được gọi. Lỗi xảy ra trước checkout, tại bước Duolingo chọn package từ catalog.

Deep link nội bộ `duolingo://purchase_max` cũng không khắc phục được. DEX cho thấy route này được ghi nhận với context `MAX_DEEP_LINK` rồi viết lại sang `duolingo://max`. Khi preview Family đang hoạt động, thử nghiệm còn mở `SuperFamilyPlanDirectAddDialogFragment` với `is_immersive_max_family_plan=true`, không phải màn hình mua hàng.

### Cách khôi phục an toàn

1. Không gọi lại `ImmersiveMaxFamilyPlanOwnerOnboardingActivity` và không xóa tiếp DataStore để cố hủy preview.
2. Chờ preview server hết hạn lúc `2026-11-16 15:35:12` (Asia/Bangkok), sau đó force-stop/mở lại app để đồng bộ user và catalog mới.
3. Trước khi thử mua, kiểm tra response user không còn `com.duolingo.immersive_gold_free_trial_family_subscription` và catalog đã có ít nhất một package `type: "gold"`.
4. Chỉ khi cả hai điều kiện trên đúng mới mở flow Max và dừng trước bước xác nhận thanh toán để đối chiếu đúng product ID/giá.

Nếu cần mua ngay, phương án hợp lệ là dùng một tài khoản Duolingo khác đủ eligibility hoặc nhờ Duolingo Support gỡ preview nội bộ khỏi tài khoản. Không có thao tác local nào hủy được entitlement server này.

## Kiểm tra tài khoản thứ hai ngày 2026-11-13

Tài khoản thứ hai không có Plus đang hoạt động (`hasPlus: false`) và không bị preview Max Family 72 giờ của tài khoản đầu tiên. Tuy nhiên tài khoản này có dấu vết một gói Family Super nội bộ cũ trong `subscriptionConfigs`:

```text
productId: com.duolingo.immersive_free_trial_family_subscription
renewer: IMMERSIVE_PLUS
```

Catalog server mới tạo cho tài khoản gồm:

```text
super.wbt7noft.1m.26q2litertr.1699   type=premium
super.wbt7.12m.26q2litertr.11999     type=premium
lite.wbt7.12m.26q2litertr.3599       type=lite
super.wbt7fp.12m.26q3fp.16799        type=premium, family=true
```

Không có package `type: "gold"`. Tiền tố `wbt7` có khả năng là cohort win-back trial 7 ngày; đây là suy luận từ tên product, không phải tên experiment được server công khai.

### Loại trừ nguyên nhân do khóa học

Ban đầu tài khoản dùng `DUOLINGO_ZH-CN_VI`. Tài khoản cũng đã có sẵn `DUOLINGO_ES_EN`, nên đã chuyển tạm sang khóa học Tây Ban Nha từ tiếng Anh mà không thêm khóa học mới. Server tạo catalog riêng cho `VN/DUOLINGO_ES_EN`, nhưng danh sách package vẫn giống hệt: chỉ `premium`/`lite`, không có `gold`.

Luồng thực tế trên khóa học tiếng Anh nguồn:

```text
TRY FOR ₫0.00
→ GET DUOLINGO MAX
→ START MY FREE WEEK
→ Get started with Super today / GET SUPER
```

Google Play Billing vẫn chưa được gọi. Sau kiểm tra, course đã được chuyển lại `DUOLINGO_ZH-CN_VI` như ban đầu.

Kết luận cập nhật: màn hình giới thiệu Max và timeline trial không chứng minh tài khoản có thể mua Max. Điều kiện quyết định cuối cùng vẫn là server catalog phải chứa package `type: "gold"`. Tài khoản thứ hai cũng không có package này, nhiều khả năng do cohort/eligibility hoặc lịch sử subscription của chính tài khoản, không phải do thiết bị thiếu SKU và cũng không phải do khóa học hiện tại.

## Kiểm tra tĩnh activity preview dài hơn

Không gọi thêm activity preview vì lần gọi onboarding Max trước đã tạo entitlement thật trên server. Thay vào đó, APK/DEX bản `6.105.2` được kiểm tra tĩnh.

Các product immersive nội bộ tìm thấy:

```text
com.duolingo.immersive_free_trial_subscription
com.duolingo.immersive_free_trial_family_subscription
com.duolingo.immersive_extended_free_trial_subscription
com.duolingo.immersive_gold_free_trial_family_subscription
```

Chỉ product cuối chứa `gold` và được dùng bởi preview Max Family đã quan sát; server cấp đúng 72 giờ. Không tìm thấy product preview Max/Gold thứ hai có thời hạn dài hơn.

APK có `ExtendedImmersiveForTopLearnersActivity`. DEX xác nhận activity này mua product `com.duolingo.immersive_extended_free_trial_subscription`, tracking type `immersive_super_extended`. UI lấy số ngày từ state server/runtime và dùng mặc định `21` với chuỗi `You're a top learner ... enjoy <num> day of Super on us`.

Vì vậy activity dài hơn là preview **Super 21 ngày**, không phải Max. Nó còn phụ thuộc eligibility “top learner” và có thể tạo entitlement server nếu được kích hoạt; không được gọi trực tiếp để tránh làm thay đổi tài khoản hoặc khóa tiếp checkout. Thời hạn không phải tham số Intent có thể tự đặt.

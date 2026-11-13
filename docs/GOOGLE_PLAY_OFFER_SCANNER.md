# Trình quét Google Play Billing Offer

## Mục tiêu

Chức năng `Google Play: Quét mọi Offer` dùng để kiểm tra toàn bộ ProductDetails mà Google Play đã trả về cho một ứng dụng trên thiết bị. Đây là thao tác chỉ đọc, phục vụ phân tích base plan, trial và khuyến mãi theo tài khoản Play hiện tại.

## Nguồn dữ liệu

Google Play Store lưu cache tại:

```text
/data/data/com.android.vending/databases/sku_details.db
```

Bảng được đọc là `cache_table`. Trình quét chỉ chọn các dòng có:

```text
callingPackageName=<package>
skuType=subs hoặc skuType=inapp
```

`cache_data` là BLOB protobuf có chứa JSON ProductDetails. Tool đọc BLOB dưới dạng hex, giải mã UTF-8, tìm JSON đầu tiên bằng bộ đếm dấu ngoặc có xử lý chuỗi escape rồi phân tích JSON bằng `JavaScriptSerializer`.

## Trường được trích xuất

Với subscription:

- `productId`
- `subscriptionOfferDetails[].basePlanId`
- `subscriptionOfferDetails[].offerId`
- `subscriptionOfferDetails[].offerTags[]`
- `pricingPhases[].formattedPrice`
- `pricingPhases[].priceCurrencyCode`
- `pricingPhases[].billingPeriod`
- `pricingPhases[].recurrenceMode`
- `pricingPhases[].billingCycleCount`

Offer không có `offerId` được ghi là `(base plan)`. Mỗi pricing phase là một dòng riêng để thể hiện đúng cấu trúc như giá khuyến mãi tháng đầu rồi quay về giá định kỳ.

Tool cũng hỗ trợ dữ liệu one-time product khi ProductDetails có `oneTimePurchaseOfferDetails`, cùng dạng giá one-time cũ ở cấp product.

## Bảo vệ dữ liệu

Kết quả không ghi hoặc hiển thị:

- `accountName` trong cache key.
- `offerIdToken`.
- `skuDetailsToken`.
- Purchase token hoặc thông tin đăng nhập.

CSV chỉ chứa thông tin sản phẩm, giá, chu kỳ, tag, serial thiết bị và thời điểm cache hết hạn.

## Quy trình sử dụng

1. Mở ứng dụng cần kiểm tra và vào paywall để BillingClient gọi ProductDetails.
2. Trong GUI, chọn thiết bị.
3. Bấm `Google Play: Quét mọi Offer`.
4. Nhập package, ví dụ `com.openai.chatgpt` hoặc `com.photoroom.app`.
5. Xem bảng kết quả hoặc mở CSV tại `%LOCALAPPDATA%\PicsartResetTool\Reports\Offers`.

Nếu không có kết quả, cache có thể chưa được tạo, đã hết hạn hoặc ứng dụng chưa truy vấn sản phẩm trên tài khoản Play hiện tại.

## Giới hạn

- Kết quả chỉ phản ánh Play country và tài khoản Google Play đang hoạt động trên thiết bị.
- Offer có trong ProductDetails chưa chắc được app chọn; app/server có thể áp dụng điều kiện riêng.
- Tool không thể lấy danh sách cấu hình mọi quốc gia của một app bên thứ ba.
- Tool không sửa giá, không chọn offer token, không mở thanh toán và không cấp entitlement.

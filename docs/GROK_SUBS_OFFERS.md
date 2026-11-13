# Phân tích subscription và offer Grok

Ngày kiểm tra: 2026-11-14  
Thiết bị: `21f11460800c7ece`  
Package: `ai.x.grok`  
Phiên bản: `1.2.48-release.00` (`versionCode=11248200`)

## Kết luận nhanh

Paywall của Grok đang hiển thị giá theo Thổ Nhĩ Kỳ (TRY) và quảng cáo giảm 50% tháng đầu cho ba hạng `Premium`, `Super` và `Ultra`. Tuy nhiên Google Play Billing trả `UnfetchedProduct statusCode=4` cho cả bốn subscription product.

Theo Android Billing Library, mã `4` của `UnfetchedProduct.StatusCode` là `NO_ELIGIBLE_OFFER`: sản phẩm tồn tại nhưng Google Play không tìm thấy base plan/offer nào mà tài khoản Play hiện tại đủ điều kiện nhận. Vì vậy tại thời điểm kiểm tra **không có offer Grok nào có thể dùng để checkout trên tài khoản Play hiện tại**, dù paywall Grok vẫn dựng được giá và banner khuyến mãi.

Tài liệu tham chiếu chính thức: [UnfetchedProduct.StatusCode](https://developer.android.com/reference/com/android/billingclient/api/UnfetchedProduct.StatusCode).

## Ánh xạ hạng hiển thị và Product ID

Ánh xạ được đối chiếu từ enum subscription trong DEX của APK:

| Hạng trên UI | Product ID Google Play | Tên nội bộ |
|---|---|---|
| Plus | `grok.lite` | `SUPER_GROK_LITE` |
| Premium | `grok.plus` | `SUPER_GROK_PLUS` |
| Super | `grok.pro` | `SUPER_GROK` |
| Ultra | `grok.ultra` | `SUPER_GROK_HEAVY` |

## Giá và ưu đãi được paywall hiển thị

| Hạng | Hàng tháng | Hàng năm | Khuyến mãi nhìn thấy |
|---|---:|---:|---|
| Plus | TRY 379.99/tháng | TRY 3,799.90/năm (xấp xỉ TRY 317/tháng) | Không có giảm giá tháng đầu; UI ghi tiết kiệm 17% khi mua năm |
| Premium | TRY 874.99 tháng đầu, sau đó TRY 1,749.99/tháng | TRY 17,499.90/năm (xấp xỉ TRY 1,459/tháng) | Giảm 50% tháng đầu |
| Super | TRY 3,149.99 tháng đầu, sau đó TRY 6,299.99/tháng | TRY 62,999.90/năm (xấp xỉ TRY 5,250/tháng) | Giảm 50% tháng đầu |
| Ultra | TRY 6,249.99 tháng đầu, sau đó TRY 12,499.99/tháng | TRY 124,999.90/năm (xấp xỉ TRY 10,417/tháng) | Giảm 50% tháng đầu |

CTA của ba hạng có giảm giá ghi `Claim limited-time offer`. Đây mới là nội dung do paywall Grok hiển thị; nó không chứng minh Google Play đã cấp một `offerToken` hợp lệ cho tài khoản.

## Kết quả truy vấn Google Play Billing

Sau khi bật log chi tiết của BillingClient, force-stop và mở lại Grok, ứng dụng truy vấn đủ bốn product nhưng nhận cùng một kết quả:

```text
grok.lite   -> UnfetchedProduct statusCode=4
grok.plus   -> UnfetchedProduct statusCode=4
grok.pro    -> UnfetchedProduct statusCode=4
grok.ultra  -> UnfetchedProduct statusCode=4
```

`sku_details.db` của Google Play cũng không có ProductDetails Grok được cache sau truy vấn này. Điều đó phù hợp với việc BillingClient không nhận được subscription offer đủ điều kiện để trả về cho app.

## Diễn giải nguyên nhân

Hai lớp dữ liệu đang không đồng nhất:

```text
Grok backend/cache paywall
        ↓
Hiển thị giá TRY và banner giảm 50%
        ↓
Google Play Billing kiểm tra tài khoản/storefront
        ↓
NO_ELIGIBLE_OFFER cho cả bốn product
        ↓
Không có offer token hợp lệ để checkout
```

Khả năng cao là storefront/country hoặc điều kiện của tài khoản Google Play hiện tại không khớp catalog TRY mà Grok đang hiển thị. Đây là suy luận từ sự lệch giữa paywall và kết quả Billing; log không cho biết chính xác điều kiện nào bị thiếu. Chỉ đổi VPN hoặc ép UI hiển thị paywall không thể tự tạo eligibility ở Google Play.

Muốn kiểm tra lại một cách hợp lệ, cần mở Grok bằng tài khoản Google Play có country/payment profile phù hợp với catalog mục tiêu, sau đó để app truy vấn lại ProductDetails. Chỉ khi BillingClient trả về `ProductDetails` cùng `subscriptionOfferDetails` thì offer đó mới thực sự có khả năng đi tới checkout.

## Phạm vi an toàn

- Chỉ đọc package/APK, quan sát paywall và log BillingClient.
- Không sửa dữ liệu Grok hoặc Google Play.
- Không chọn offer token, không mở/xác nhận giao dịch và không tạo entitlement.
- Không ghi vào tài liệu account identifier, purchase token hoặc token thanh toán.

# Inapp Patch

Công cụ WinForms/ADB hỗ trợ kiểm tra và điều chỉnh cấu hình paywall cục bộ trên thiết bị Android đã root. Bản hiện tại có các thao tác cho Picsart, Grammarly, Chess.com và trình quét offer Google Play chỉ đọc.

## Quét mọi offer Google Play

Nút **`Google Play: Quét mọi Offer`** đọc cache ProductDetails của package do người dùng nhập (mặc định `com.openai.chatgpt`) và hiển thị:

- Product ID, base plan ID và offer ID.
- Mọi pricing phase, giá bản địa hóa, loại tiền, chu kỳ và kiểu lặp.
- Offer tags như `rc-ignore-offer`.
- Thời điểm hết hạn của bản cache.

Kết quả được lưu thành CSV trong `%LOCALAPPDATA%\PicsartResetTool\Reports\Offers`. Trình quét không xuất tài khoản Google, `offerIdToken` hay `skuDetailsToken`, đồng thời không sửa database và không kích hoạt offer.

Trước khi quét, hãy mở ứng dụng đích và paywall ít nhất một lần để Google Play tải ProductDetails. Tài liệu kỹ thuật: [docs/GOOGLE_PLAY_OFFER_SCANNER.md](docs/GOOGLE_PLAY_OFFER_SCANNER.md).

## Chess.com: bỏ trial và mua thẳng

Nút **`Chess: Bỏ Trial, Mua Thẳng`** vô hiệu hóa các offer dùng thử 7 ngày trong cache Google Play Billing của riêng `com.chess`, đồng thời giữ nguyên base plan, giá và offer token để người dùng thanh toán trực tiếp.

Kết quả đã kiểm tra trên thiết bị thử nghiệm:

- Paywall hiện `Go Premium`.
- Không còn `Get 7 Days of Premium for Free`.
- Không còn `Try for PHP 0.00`.
- Không tự bấm nút mua hoặc xác nhận giao dịch.

Tài liệu kỹ thuật:

- Tổng hợp cơ chế Picsart và Grammarly: [docs/IMPLEMENTATION_NOTES.md](docs/IMPLEMENTATION_NOTES.md).
- Chess.com, quy trình backup, xác minh và rollback: [docs/CHESS_NO_TRIAL.md](docs/CHESS_NO_TRIAL.md).

## Yêu cầu

- Windows và .NET Framework 4.x.
- `adb` có trong `PATH`.
- Thiết bị/emulator Android cho phép `adb root`.
- Có `/system/bin/sqlite3` để dùng tính năng Chess.com.
- Mở Chess.com và trang Premium ít nhất một lần để Google Play tạo cache sản phẩm.

## Build

Có thể build trực tiếp bằng compiler .NET Framework có sẵn trên Windows:

```powershell
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /out:PicsartToolGUI.exe `
  /reference:System.Windows.Forms.dll `
  /reference:System.Drawing.dll `
  /reference:System.Web.Extensions.dll `
  PicsartToolGUI.cs
```

File EXE, database thiết bị và các bản backup không được commit vì có thể chứa dữ liệu riêng tư. Hãy tự build EXE từ source.

## Sử dụng tính năng Chess

1. Kết nối thiết bị và mở Chess.com/Paywall một lần.
2. Chạy `PicsartToolGUI.exe`.
3. Chọn đúng thiết bị trong bảng.
4. Bấm `Chess: Bỏ Trial, Mua Thẳng`.
5. Mở paywall và kiểm tra nút hiển thị `Go Premium` trước khi quyết định mua.

Nếu đổi tài khoản Google, cập nhật Play Store hoặc Play Billing tạo cache key mới, hãy chạy lại nút Chess.

## Phạm vi an toàn

Cơ chế Chess chỉ chọn base plan có sẵn để thanh toán đủ giá. Nó không tạo giao dịch giả, không sửa giá, không sửa offer token, không cấp quyền Premium và không tự xác nhận thanh toán.


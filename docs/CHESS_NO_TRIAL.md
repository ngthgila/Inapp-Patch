# Ghi chú triển khai: Chess.com mua thẳng không qua trial

Tài liệu này lưu lại cơ chế đã phân tích và cách `PicsartToolGUI.cs` triển khai nút Chess để có thể bảo trì về sau.

## 1. Kết luận quan trọng

Không sửa byte `30 01` trong:

```text
/data/data/com.chess/files/datastore/membershipSnapshotStore.db
```

DEX/protobuf cho thấy field 6 trong `MembershipSnapshotProto` là `canExtendPrepaidPlan`, không phải `is_trial_eligible`. Đổi `30 01` thành `30 00` không tắt trial và có thể làm sai trạng thái prepaid.

Nguồn quyết định trial thực tế là danh sách offer trả về từ Google Play Billing.

## 2. Luồng chọn offer trong Chess.com

Phân tích DEX cho thấy:

1. Google Billing Repository đọc `ProductDetails` từ Play Billing.
2. Mỗi `subscriptionOfferDetails` được ánh xạ thành `PlanDataProto`, gồm `productId`, `offerId`, giá, discount và offer token.
3. Khi gom các offer theo `productId`, Chess chọn theo thứ tự:
   - Offer ID nằm trong incentive từ server.
   - Offer ID chính xác bằng `freetrial`.
   - Base plan có `offerId == null`.
   - Offer đầu tiên nếu không tìm thấy các trường hợp trên.
4. Checkout dùng offer token của plan đã chọn để mở Google Play Billing.

Vì vậy, chỉ xóa cờ UI hoặc sửa membership snapshot không đủ. Cần làm cho hai họ offer trial không còn khớp, để logic tự rơi xuống base plan.

## 3. Vị trí cache Google Play

Database:

```text
/data/data/com.android.vending/databases/sku_details.db
```

Các file đi kèm cần backup cùng lúc nếu tồn tại:

```text
sku_details.db-wal
sku_details.db-shm
```

Schema liên quan:

```sql
CREATE TABLE cache_table (
    cache_key TEXT NOT NULL PRIMARY KEY,
    cache_data BLOB NOT NULL,
    expiration_timestamp INTEGER NOT NULL,
    last_accessed_timestamp INTEGER NOT NULL,
    space_occupied_in_bytes INTEGER NOT NULL
);
```

`cache_data` là protobuf wrapper chứa chuỗi JSON ProductDetails. Trong JSON có `subscriptionOfferDetails`, `offerIdToken`, `basePlanId`, `offerId` và `pricingPhases`.

## 4. Hai dạng offer trial cần xử lý

Trên các SKU Diamond/Gold/Platinum quan sát được hai họ ID:

```text
freetrial
<product-name>free7days
```

Ví dụ:

```text
diamondyearly032022free7days
goldmonthly032022free7days
platinumyearly032022free7days
```

Chỉ đổi `freetrial` là chưa đủ, vì incentive server có thể trỏ tới offer `*free7days` và Chess sẽ chọn nó trước.

## 5. Cách patch

Giới hạn update vào cache của `com.chess` và sản phẩm `subs`:

```sql
cache_key LIKE '%callingPackageName=com.chess%'
AND cache_key LIKE '%skuType=subs%'
```

Thực hiện hai phép thay thế cùng độ dài:

```text
"offerId":"freetrial"  -> "offerId":"notrialxx"
free7days               -> no7trials
```

Cả `freetrial`/`notrialxx` và `free7days`/`no7trials` đều dài 9 ký tự. Giữ nguyên độ dài là điều quan trọng vì JSON nằm trong một BLOB có framing protobuf; thay đổi số byte mà không encode lại length field sẽ làm hỏng cache.

SQL tương đương phần chính trong app:

```sql
BEGIN IMMEDIATE;

UPDATE cache_table
SET cache_data = CAST(
        replace(
            replace(
                CAST(cache_data AS TEXT),
                '"offerId":"freetrial"',
                '"offerId":"notrialxx"'
            ),
            'free7days',
            'no7trials'
        ) AS BLOB
    ),
    expiration_timestamp = 4102444800000
WHERE cache_key LIKE '%callingPackageName=com.chess%'
  AND cache_key LIKE '%skuType=subs%'
  AND (
      instr(cache_data, '"offerId":"freetrial"') > 0
      OR instr(cache_data, 'free7days') > 0
      OR instr(cache_data, '"offerId":"notrialxx"') > 0
      OR instr(cache_data, 'no7trials') > 0
  );

COMMIT;
PRAGMA wal_checkpoint(FULL);
```

`expiration_timestamp` được đưa tới `2100-01-01 UTC` để Play Store tiếp tục dùng dòng cache đã patch. Đây không phải bảo đảm vĩnh viễn: đổi account, version Billing/Play Store hoặc query parameters có thể sinh cache key mới.

## 6. Quy trình trong GUI

Hàm triển khai chính: `PatchChessNoTrial` trong `PicsartToolGUI.cs`.

Trình tự:

1. `adb root` được gọi trước khi chạy action.
2. Preflight kiểm tra:
   - `com.chess` đã cài.
   - `sku_details.db` tồn tại.
   - Thiết bị có `sqlite3`.
3. Force-stop `com.chess` và `com.android.vending`.
4. Pull bản gốc của DB/WAL/SHM về:

   ```text
   %LOCALAPPDATA%\PicsartResetTool\Backups\Chess\<serial>\<timestamp>\
   ```

5. Tạo file SQL cục bộ và push vào `/data/local/tmp/`.
6. Chạy transaction SQLite, checkpoint WAL và xóa file SQL tạm trên thiết bị.
7. Pull database sau patch để lưu đối chiếu.
8. Khởi động lại Chess.com.

App không chỉnh `membershipSnapshotStore.db` và không xóa các feature flag trong `com.chess.app.session_preferences.xml`.

## 7. Điều kiện xác minh bắt buộc

App chỉ báo thành công khi tất cả điều kiện sau đạt:

- Có ít nhất một dòng cache subscription cho `com.chess`.
- `TRIAL_REMAINING=0`.
- Có ít nhất một dòng chứa marker `notrialxx` hoặc `no7trials`.
- Vẫn có ít nhất một dòng chứa `basePlanId`.
- Tổng `length(cache_data)` trước và sau patch bằng nhau.

Kiểm tra UI cuối cùng:

```text
Go Premium                         = có
Try for PHP 0.00                   = không
Get 7 Days of Premium for Free     = không
```

Không cần và không nên bấm xác nhận giao dịch trong quá trình test.

## 8. SKU đã kiểm tra

Kết quả trên dữ liệu thử nghiệm:

| Nhóm | Dòng subscription | Dòng có trial đã patch | Trial còn lại |
|---|---:|---:|---:|
| Diamond | 2 | 2 | 0 |
| Gold | 2 | 2 | 0 |
| Platinum | 3 | 2 | 0 |

Platinum có thêm một SKU prepaid vốn không có trial nên không cần thay đổi.

## 9. Rollback

Để khôi phục cache trước patch:

1. Force-stop `com.chess` và `com.android.vending`.
2. Dùng đúng bộ `sku_details.before.db`, `-wal`, `-shm` trong cùng thư mục timestamp.
3. Push lại đủ các file đang có vào thư mục database của `com.android.vending`.
4. Khôi phục owner/mode/SELinux context theo file gốc.
5. Khởi động Play Store hoặc Chess.com.

Không trộn DB của timestamp này với WAL/SHM của timestamp khác.

## 10. Khi cần chạy lại

Chạy lại nút Chess nếu:

- Đổi tài khoản Google Play.
- Cập nhật Play Store hoặc Billing Library.
- Cài lại Chess.com.
- Dọn dữ liệu Play Store.
- Paywall xuất hiện lại nút trial.

Patch được thiết kế idempotent: marker đã đổi vẫn được nhận diện, cache được xác minh lại và thời hạn được gia hạn.


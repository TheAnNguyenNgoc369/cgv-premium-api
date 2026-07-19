# CinemaBooking API report cho Frontend

> Cập nhật trực tiếp từ source ngày **05/07/2026**. Hiện có **94 endpoint** trong 18 controller. Tài liệu này ưu tiên route, quyền truy cập, query parameter và các giá trị trạng thái để FE tích hợp.

## 1. Quy ước chung

- Base URL local tùy `launchSettings.json`; mọi route bên dưới bắt đầu bằng `/api`.
- Endpoint có bảo vệ dùng header `Authorization: Bearer <JWT>`.
- Role trong JWT: `customer`, `staff`, `manager`, `admin`.
- JSON dùng `Content-Type: application/json`; upload dùng `multipart/form-data` với field `file`, riêng voucher dùng field `image`.
- `startTime` của showtime phải là ISO 8601 kèm đúng offset Việt Nam `+07:00`, ví dụ `2026-07-05T19:30:00+07:00`.
- Ngày query report/showtime dùng `yyyy-MM-dd`.
- Response thời gian ở API boundary được serialize theo giờ Việt Nam `+07:00`; nội bộ lưu/xử lý UTC.
- Với lỗi nghiệp vụ, shape phổ biến là `{"success":false,"message":"..."}`. Lỗi DataAnnotations/model binding có thể trả `ValidationProblemDetails`.
- `204 No Content` không có response body.

### Phân quyền ký hiệu

| Ký hiệu | Ý nghĩa |
|---|---|
| Public | Không cần token |
| Auth | Chỉ cần token hợp lệ |
| Customer/Staff | Chỉ role `customer` hoặc `staff` |
| Manager | Chỉ role `manager`; dữ liệu quản lý bị giới hạn theo cinema được gán |
| Admin | Chỉ role `admin` |
| Admin/Manager | Cả hai role; Manager luôn bị ép về cinema được gán |

## 2. Query parameters FE cần dùng

### `GET /api/admin/users`

| Query | Kiểu | Mặc định | Ghi chú |
|---|---:|---:|---|
| `search` | string? | null | Từ khóa tìm user |
| `role` | string? | null | `customer`, `staff`, `manager`, `admin` |
| `status` | string? | null | `unverified`, `active`, `locked`, `inactive` |
| `page` | int | `1` | Phải `>= 1` |
| `pageSize` | int | `10` | Phải trong `1..100` |

Ví dụ: `/api/admin/users?search=an&role=staff&status=active&page=1&pageSize=20`.

### `GET /api/movie`

| Query | Kiểu | Mặc định | Ghi chú |
|---|---:|---:|---|
| `status` | string? | null | `coming_soon`, `now_showing`, `ended` |
| `genreId` | CSV string? | null | Danh sách ID, ví dụ `1,2,3` |
| `genreName` | CSV string? | null | Danh sách tên, ví dụ `Action,Drama` |
| `pageIndex` | int | `1` | Phải `>= 1` |
| `pageSize` | int | `10` | Phải trong `1..100` |

Response phân trang: `items`, `totalCount`, `pageIndex`, `pageSize`. Item list còn có `ticketsSold`, `isTopSelling`, `salesRank`.

### `GET /api/movie/search`

| Query | Bắt buộc | Ghi chú |
|---|---|---|
| `keyword` | Có | Không được rỗng; tìm theo tên phim |

### `GET /api/showtimes`

| Query | Kiểu | Mặc định | Ghi chú |
|---|---:|---:|---|
| `movieId` | int? | null | Lọc theo phim |
| `cinemaId` | int? | null | Lọc theo rạp; Manager không được xem cinema khác |
| `movieName` | string? | null | Tìm theo tên phim |
| `roomName` | string? | null | Tìm theo tên phòng |
| `date` | date? | null | `yyyy-MM-dd`, theo ngày Việt Nam |
| `status` | string? | null | `scheduled`, `completed`, `cancelled` |
| `page` | int | `1` | Phải `>= 1` |
| `pageSize` | int | `10` | Phải trong `1..100` |
| `sortBy` | string | `startTime` | `startTime`, `endTime`, `basePrice`, `status`, `id` |
| `sortDir` | string | `asc` | `asc`, `desc` |

Response phân trang: `items`, `page`, `pageSize`, `totalItems`, `totalPages`. Mỗi item có `movie`, `room`, `cinema`, `startTime`, `endTime`, `basePrice`, `status`, `isSoldOut`.

### `GET /api/rooms/{roomId}/seats`

| Query | Kiểu | Ghi chú |
|---|---:|---|
| `seatId` | int? | Lấy đúng một vị trí; phải `> 0` |
| `rows` | CSV string? | Lọc hàng, ví dụ `A,B,C` |
| `columns` | CSV string? | Lọc cột, ví dụ `1,2,3`; từng giá trị phải là số nguyên dương |

Có thể kết hợp các filter. Response luôn là danh sách seat/gap với `seatId`, `roomId`, `rowLabel`, `seatNumber`, `seatCode`, `seatTypeId`, `type`, `status`, `isGap`.

### `GET /api/products/available`

| Query | Kiểu | Ghi chú |
|---|---:|---|
| `showtimeId` | int | Bắt buộc; dùng để kiểm tra showtime hợp lệ trước khi trả sản phẩm đang bán |

### `GET /api/vouchers`

| Query | Kiểu | Mặc định | Ghi chú |
|---|---:|---:|---|
| `pageIndex` | int | `1` | Trang hiện tại |
| `pageSize` | int | `10` | Số item/trang |
| `searchKeyword` | string? | null | Tìm theo mã voucher |

Response: `items`, `pageIndex`, `pageSize`, `totalItems`, `totalPages`.

### Report: `/api/v1/reports/*`

| Query | Áp dụng | Bắt buộc | Ghi chú |
|---|---|---|---|
| `startDate` | tất cả | Có | `yyyy-MM-dd` |
| `endDate` | tất cả | Có | `yyyy-MM-dd`, không trước `startDate` |
| `cinemaId` | tất cả | Không | Admin có thể chọn; Manager bị ép về cinema được gán |
| `searchMovie` | `movie-performance` | Không | Tìm theo tên phim |
| `format` | `export` | Có | `excel` hoặc `pdf` |
| `reportType` | `export` | Có | `revenue`, `fnb`, `occupancy` |

Khoảng report tính từ đầu `startDate` đến trước đầu ngày sau `endDate`, theo giờ Việt Nam.

### Callback query của VNPay

`POST /api/payments/vnpay/callback` nhận toàn bộ query do VNPay gửi (`vnp_*`), không có body. FE không nên tự dựng callback này; chỉ chuyển người dùng tới payment URL và xử lý kết quả điều hướng theo flow của gateway.

## 3. Trạng thái và giá trị hợp lệ

API enum không phân biệt hoa/thường khi đi qua mapper, nhưng FE nên gửi đúng value dưới đây để contract ổn định.

| Nhóm | Giá trị API |
|---|---|
| User role | `customer`, `staff`, `manager`, `admin` |
| User status | `unverified`, `active`, `locked`, `inactive` |
| Cinema status | `active`, `inactive`, `maintenance` |
| Room type | `STANDARD`, `VIP`, `IMAX`, `THREE_D` (response chuẩn hóa uppercase; DB `3D`) |
| Room status | `active`, `maintenance`, `inactive` |
| Seat configuration status | `active`, `inactive` |
| Seat map runtime status | `available`, `held`, `booked` |
| Seat hold status | `holding`, `confirmed`, `released`, `expired` |
| Movie status | `coming_soon`, `now_showing`, `ended` |
| Movie age rating | `P`, `C13`, `C16`, `C18` |
| Showtime status | `scheduled`, `completed`, `cancelled` |
| Booking status | `pending`, `paid`, `cancelled`, `refunded`, `used`, `expired`, `payment_failed`, `partially_refunded` |
| Payment method được mapper hỗ trợ | `wallet`, `vnpay`, `payos`, `momo`, `credit_card`, `banking`, `cash` |
| Payment method hiện có flow backend | `wallet`, `vnpay`, `payos`, `cash` (`cash` chỉ Staff) |
| Payment status | `pending`, `success`, `failed`, `refunded`, `cancelled`, `expired` |
| Payment session status | `waiting`, `processing`, `completed`, `expired`, `cancelled` |
| Ticket status | `valid`, `used`, `cancelled` |
| Product type | `combo`, `snack`, `beverage`, `dessert` |
| Product status | `active`, `inactive` |
| Voucher discount type | `percent`, `fixed` |
| Voucher category | `Discount`, `Combo`, `Cashback` |
| Loyalty transaction | `earn`, `redeem`, `expire`, `adjust` |
| Loyalty tier | `silver`, `gold`, `platinum`, `megavip` |

Lưu ý quan trọng:

- Payment thành công là `success`, không phải `completed`.
- `POST /api/showtimes` tự tính status: tương lai là `scheduled`, thời điểm đã qua là `completed`.
- `PUT /api/showtimes/{id}` cho phép bỏ `status` để giữ nguyên; nếu gửi thì chỉ dùng ba giá trị showtime ở bảng trên.
- Xóa showtime có booking/seat-hold đang hoạt động hoặc lịch sử liên quan có thể trả `409 Conflict`.
- Product là dữ liệu global do Admin quản lý; endpoint `available` không nhận `cinemaId`.

## 4. Danh sách 94 endpoint theo source

### 4.1 Authentication — 7

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| POST | `/api/auth/register` | Public | `fullName`, `email`, `phone`, `password`, `confirmPassword` |
| POST | `/api/auth/resend-verification-email` | Public | `email` |
| POST | `/api/auth/forgot-password` | Public | `email` |
| POST | `/api/auth/reset-password` | Public | `token`, `newPassword`, `confirmPassword` |
| POST | `/api/auth/login` | Public | `email`, `password`, `rememberMe` |
| POST | `/api/auth/logout` | Auth | Không body; thu hồi token hiện tại |
| POST | `/api/auth/verify-email` | Public | `code` |

### 4.2 User profile — 6

| Method | Route | Quyền | Body / response chính |
|---|---|---|---|
| GET | `/api/user/profile` | Auth | Profile; Manager/Staff có thêm `cinema` nếu được gán |
| PUT | `/api/user/profile` | Auth | `fullName`, `phone` |
| PUT | `/api/user/profile/avatar` | Auth | Multipart `file` |
| DELETE | `/api/user/profile/avatar` | Auth | Xóa avatar |
| GET | `/api/user/wallet` | Auth | `walletId`, `balance` |
| PUT | `/api/user/password` | Auth | `oldPassword`, `newPassword`, `confirmPassword` |

### 4.3 Admin user management — 9

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/admin/users` | Admin | Query tại mục 2 |
| POST | `/api/admin/users` | Admin | Tạo Staff/Manager/Admin; customer không được tạo từ API này |
| PUT | `/api/admin/users/{id}` | Admin | `fullName`, `email`, `phone`, `cinemaId` |
| PATCH | `/api/admin/users/{id}/role` | Admin | `role`, `cinemaId` |
| PATCH | `/api/admin/users/{id}/status` | Admin | `status` |
| PATCH | `/api/admin/users/{id}/password` | Admin | `password`, `confirmPassword` |
| PUT | `/api/admin/users/{id}/avatar` | Admin | Multipart `file` |
| DELETE | `/api/admin/users/{id}/avatar` | Admin | Xóa avatar |
| DELETE | `/api/admin/users/{id}` | Admin | `204` hoặc deactivate tùy quan hệ dữ liệu |

### 4.4 Cinema — 5

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/cinemas` | Public | Danh sách cinema |
| GET | `/api/cinemas/{id}` | Public | Chi tiết cinema |
| POST | `/api/cinemas` | Admin | `cinemaName`, `address`, `status` |
| PUT | `/api/cinemas/{id}` | Admin | `cinemaName`, `address`, `status` |
| DELETE | `/api/cinemas/{id}` | Admin | Soft delete về inactive |

### 4.5 Room — 5

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/rooms` | Customer/Staff/Admin/Manager | Manager chỉ thấy room thuộc cinema được gán |
| GET | `/api/rooms/{id}` | Customer/Staff/Admin/Manager | Chi tiết room |
| POST | `/api/rooms` | Manager | `cinemaId`, `name`, `type`, `status`, `description` |
| PUT | `/api/rooms/{id}` | Manager | Cùng shape create |
| DELETE | `/api/rooms/{id}` | Manager | Bị chặn nếu có lịch sử showtime |

### 4.6 Seat — 4

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/rooms/{roomId}/seats` | Public | Query `seatId`, `rows`, `columns` |
| POST | `/api/rooms/{roomId}/seats/generate` | Manager | Tạo dải ghế: row, cột bắt đầu/kết thúc, seat type, status |
| PATCH | `/api/rooms/{roomId}/seats/bulk` | Manager | `selectors` + field cần đổi: `seatTypeId`, `status`, `isGap` |
| DELETE | `/api/rooms/{roomId}/seats/bulk` | Manager | `selectors`; xóa/ẩn theo quan hệ lịch sử |

Seat selector hỗ trợ chọn theo `seatId` hoặc cặp vị trí hàng/cột. Mutation chỉ được thực hiện khi room ở trạng thái `inactive`; layout tối đa 100 hàng, 100 cột và 10.000 vị trí.

### 4.7 Seat type — 5

| Method | Route | Quyền | Body |
|---|---|---|---|
| GET | `/api/seat-types` | Manager | Danh sách |
| GET | `/api/seat-types/{id}` | Manager | Chi tiết |
| POST | `/api/seat-types` | Manager | `typeName`, `capacity`, `extraPrice` |
| PUT | `/api/seat-types/{id}` | Manager | Cùng shape create |
| DELETE | `/api/seat-types/{id}` | Manager | `204` khi thành công |

### 4.8 Genre — 5

| Method | Route | Quyền | Body |
|---|---|---|---|
| GET | `/api/genres` | Public | Danh sách |
| GET | `/api/genres/{id}` | Public | Chi tiết |
| POST | `/api/genres` | Admin | `genreName` |
| PUT | `/api/genres/{id}` | Admin | `genreName` |
| DELETE | `/api/genres/{id}` | Admin | `204` khi thành công |

### 4.9 Movie — 7

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/movie` | Public | Filter + phân trang tại mục 2 |
| GET | `/api/movie/{id}` | Public | Chi tiết phim |
| GET | `/api/movie/search` | Public | Query `keyword` |
| POST | `/api/movie` | Admin | Không nhận `status`; backend tự tính theo ngày chiếu |
| PUT | `/api/movie/{id}` | Admin | Có thể cập nhật `status` hợp lệ |
| PUT | `/api/movie/{id}/poster` | Admin | Multipart `file` |
| DELETE | `/api/movie/{id}` | Admin | `204` khi thành công |

Movie body chính: `title`, `genres`, `ageRating`, `director`, `cast`, `synopsis`, `durationMinutes`, `showingFromDate`, `showingToDate`, `posterUrl`, `posterPublicId`, `trailerUrl`.

### 4.10 Showtime — 6

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/showtimes` | Public | Filter/sort/phân trang tại mục 2; Manager có token bị scope cinema |
| POST | `/api/showtimes` | Manager | `movieId`, `roomId`, `startTime`, `basePrice` |
| PUT | `/api/showtimes/{id}` | Manager | Thêm optional `status` |
| DELETE | `/api/showtimes/{id}` | Manager | `204`; có thể `409` nếu có quan hệ booking/hold |
| GET | `/api/showtimes/{id}` | Public | Manager có token bị scope cinema |
| GET | `/api/showtimes/{showtimeId}/seats` | Public | Seat map kèm `price` và runtime status |

`endTime = startTime + durationMin + 30 phút`.

### 4.11 Product / F&B — 7

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/products` | Admin | Toàn bộ sản phẩm, mọi status |
| GET | `/api/products/available` | Public | Query bắt buộc `showtimeId` |
| GET | `/api/products/{id}` | Public | Chi tiết sản phẩm |
| POST | `/api/products` | Admin | `itemName`, `itemType`, `description`, `price`, `imageURL`, `isLoyaltyEligible` |
| PUT | `/api/products/{id}` | Admin | Cùng create + `status` |
| DELETE | `/api/products/{id}` | Admin | Soft delete/inactive |
| PUT | `/api/products/{id}/image` | Admin | Multipart `file` |

### 4.12 Booking và seat hold — 6

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| POST | `/api/seat-holds` | Customer/Staff | `showtimeId`, `seatIds` |
| DELETE | `/api/seat-holds` | Customer/Staff | `showtimeId`, `seatIds`; fail-fast, giải phóng tất cả hoặc không ghế nào |
| POST | `/api/bookings/calculate-pricing` | Customer/Staff | `showtimeId`, `seatIds`, optional product/voucher data |
| POST | `/api/bookings` | Auth | Tạo booking từ seats, products và voucher đã chọn |
| GET | `/api/bookings/{id}` | Auth | Customer chỉ xem booking của mình; Staff theo rule service |
| GET | `/api/bookings/my` | Auth | Booking của user hiện tại |

### 4.13 Payment — 6

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| POST | `/api/payments/initiate` | Customer/Staff | `bookingId`, `paymentMethod` |
| POST | `/api/payments/cash/confirm` | Staff | `paymentId` |
| POST | `/api/payments/vnpay/callback` | Public | Query `vnp_*`, không body |
| POST | `/api/payments/payos/webhook` | Public | Webhook body do PayOS gửi; FE không gọi |
| GET | `/api/payments/{id}` | Customer/Staff | Customer chỉ xem payment của mình |
| GET | `/api/payments/booking/{bookingId}` | Customer/Staff | Payment theo booking |

`initiate` trả dữ liệu theo method: wallet/cash có kết quả trực tiếp; VNPay/PayOS có payment URL để FE redirect.

### 4.14 Invoice — 3

| Method | Route | Quyền | Ghi chú |
|---|---|---|---|
| GET | `/api/invoices/{id}` | Auth | Theo invoice ID |
| GET | `/api/invoices/booking/{bookingId}` | Auth | Theo booking ID |
| GET | `/api/invoices/code/{code}` | Auth | Theo invoice code |

### 4.15 Membership — 3

| Method | Route | Quyền | Ghi chú |
|---|---|---|---|
| GET | `/api/membership/me` | Auth | Membership user hiện tại |
| GET | `/api/membership/tiers` | Auth | Danh sách tier |
| GET | `/api/membership/points-history` | Auth | Lịch sử điểm |

### 4.16 Voucher — 4

| Method | Route | Quyền | Body / ghi chú |
|---|---|---|---|
| GET | `/api/vouchers` | Public | Query/phân trang tại mục 2 |
| POST | `/api/vouchers` | Admin | Multipart: các field voucher + optional `image` |
| PUT | `/api/vouchers/{id}` | Admin | Multipart như create |
| DELETE | `/api/vouchers/{id}` | Admin | `204` khi thành công |

Voucher fields: `voucherCode`, `category`, `discountType`, `discountValue`, `minOrderValue`, `maxUses`, `validFrom`, `validUntil`, `description`, `isActive`, `image`.

### 4.17 Ticket — 2

| Method | Route | Quyền | Ghi chú |
|---|---|---|---|
| GET | `/api/tickets/booking/{bookingId}` | Customer/Staff | Vé theo booking, có kiểm tra quyền sở hữu |
| GET | `/api/tickets/my` | Customer | Toàn bộ vé của customer hiện tại |

### 4.18 Reports — 4

| Method | Route | Quyền | Query / response |
|---|---|---|---|
| GET | `/api/v1/reports/revenue-summary` | Admin/Manager | `startDate`, `endDate`, optional `cinemaId` |
| GET | `/api/v1/reports/movie-performance` | Admin/Manager | Thêm optional `searchMovie` |
| GET | `/api/v1/reports/top-selling` | Admin/Manager | Danh sách phim theo `ticketsSold`, F&B theo `quantitySold`, và cinema theo số vé; không giới hạn top N; chỉ payment `success` |
| GET | `/api/v1/reports/export` | Admin/Manager | Thêm `format`, `reportType`; trả file binary |

## 5. HTTP status FE nên xử lý

| HTTP | Ý nghĩa thực tế |
|---:|---|
| `200` | Thành công, có body |
| `201` | Tạo thành công |
| `204` | Xóa thành công, không parse JSON |
| `400` | Query/body/enum không hợp lệ hoặc vi phạm rule đầu vào |
| `401` | Thiếu, hết hạn hoặc bị thu hồi token |
| `403` | Sai role, sai chủ sở hữu, hoặc Manager truy cập cinema khác |
| `404` | Không tìm thấy resource |
| `409` | Trùng dữ liệu, ghế vừa bị giữ, hoặc resource có quan hệ không thể sửa/xóa |
| `502` | Payment gateway lỗi |

FE nên đọc `message` khi có, nhưng không phụ thuộc duy nhất vào text tiếng Anh; nhánh xử lý chính nên dựa vào HTTP status.

## 6. Flow tích hợp đề xuất

1. Login và lưu JWT; lấy `role` và `cinema` từ response/profile để dựng menu.
2. Public catalog: cinema → movie → showtime → seat map → products available.
3. Khi chọn ghế: gọi `POST /api/seat-holds`; nếu `409`, refresh seat map ngay.
4. Gọi calculate-pricing trước khi tạo booking; không tự tính tổng tiền ở FE làm nguồn sự thật.
5. Tạo booking rồi initiate payment. Redirect nếu backend trả payment URL.
6. Poll/read payment theo booking sau khi quay lại từ gateway; chỉ coi `payment.status === "success"` là thanh toán hoàn tất.
7. Sau thành công, đọc invoice và ticket bằng booking ID.

## 7. Nguồn đối chiếu

- Route/role/query: `src/CinemaBooking.API/Controllers`.
- Request/response: `src/CinemaBooking.API/Contracts` và `src/CinemaBooking.Application/Contracts`.
- Enum API: `src/CinemaBooking.Application/Common/Enums/DatabaseEnumMappings.cs`.
- Hằng trạng thái thanh toán/booking/hold: `src/CinemaBooking.Shared/Constants`.

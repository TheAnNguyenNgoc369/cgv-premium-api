# CinemaBooking API - Request/Response cho Tester

> Cập nhật theo source ngày 05/07/2026. Source hiện có 95 API. Mỗi API bên dưới có đúng một request và một response thành công mẫu. ID, token, code và timestamp là dữ liệu minh họa; phải thay bằng dữ liệu thực tế của môi trường test.

## 1. Quy ước sử dụng

- Base URL mẫu: `https://localhost:7001`.
- API protected cần header `Authorization: Bearer <token>`.
- Role: `Public`, `Authenticated`, `Customer/Staff`, `Manager`, `Admin`.
- Body JSON dùng `Content-Type: application/json`.
- Upload file dùng `multipart/form-data` với field `file`.
- Request `startTime` của showtime phải dùng ISO 8601 với đúng offset giờ Việt Nam `+07:00`, ví dụ `2026-07-05T19:30:00+07:00`.
- Các field response kiểu ngày-giờ được trả theo giờ Việt Nam với offset `+07:00`; Application và database vẫn xử lý/lưu UTC nội bộ.
- Loại phòng hợp lệ: `Standard`, `VIP`, `IMAX`, `3D`.
- Phương thức thanh toán được triển khai: `wallet`, `vnpay`, `payos`, `cash` (cash chỉ dành cho Staff).
- Response có thể thay đổi giá trị ID/timestamp theo database nhưng phải giữ đúng shape.

### Biến dữ liệu

| Biến | Ý nghĩa |
|---|---|
| `<token>` | JWT đúng role của API |
| `<userId>` | ID user được tạo/lấy từ API |
| `<cinemaId>` | ID cinema active |
| `<roomId>` | ID room active thuộc cinema |
| `<seatTypeId>` | ID seat type |
| `<seatId>` | ID seat active thuộc room |
| `<movieId>` | ID movie `now_showing` |
| `<showtimeId>` | ID showtime `scheduled`, còn trên 15 phút |
| `<productId>` | ID product đang bán và còn hàng |
| `<bookingId>` | ID booking của user hiện tại |
| `<paymentId>` | ID payment |
| `<invoiceId>` | ID invoice |

## 2. Authentication - 7 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `POST /api/auth/register`<br>Public | `{"fullName":"QA Customer","email":"qa.customer@example.com","phone":"0901234567","password":"Test@123","confirmPassword":"Test@123"}` | `200 {"success":true,"message":"Registration successful.","userId":10,"verificationEmailSent":true}` |
| `POST /api/auth/resend-verification-email`<br>Public | `{"email":"qa.customer@example.com"}` | `200 {"success":true,"message":"Verification email sent successfully.","verificationEmailSent":true,"retryAfterSeconds":null}` |
| `POST /api/auth/forgot-password`<br>Public | `{"email":"qa.customer@example.com"}` | `200 {"success":true,"message":"Password reset email sent successfully.","emailSent":true,"retryAfterSeconds":null}` |
| `POST /api/auth/reset-password`<br>Public | `{"token":"<resetToken>","newPassword":"NewTest@123","confirmPassword":"NewTest@123"}` | `200 {"success":true,"message":"Password has been reset successfully."}` |
| `POST /api/auth/login`<br>Public | `{"email":"qa.customer@example.com","password":"Test@123","rememberMe":false}` | `200 {"success":true,"message":"Login successful.","token":"<jwt>","user":{"userID":10,"fullName":"QA Customer","email":"qa.customer@example.com","role":"customer","status":"active","avatarURL":null}}` |
| `POST /api/auth/logout`<br>Authenticated | Header: `Authorization: Bearer <token>`<br>Body: none | `200 {"success":true,"message":"Logout successful"}` |
| `POST /api/auth/verify-email`<br>Public | `{"code":"<verificationCode>"}` | `200 {"success":true,"message":"Email verified successfully"}` |

## 3. User profile - 6 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/user/profile`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"userID":10,"fullName":"QA Customer","email":"qa.customer@example.com","phone":"0901234567","role":"customer","status":"active","avatarURL":null,"totalPoints":0,"createdAt":"2026-07-01T10:00:00+07:00","cinema":null}` |
| `PUT /api/user/profile`<br>Authenticated | `{"fullName":"QA Customer Updated","phone":"0907654321"}` | `200 {"userID":10,"fullName":"QA Customer Updated","email":"qa.customer@example.com","phone":"0907654321","role":"customer","status":"active","avatarURL":null,"totalPoints":0,"createdAt":"2026-07-01T10:00:00+07:00"}` |
| `PUT /api/user/profile/avatar`<br>Authenticated | Multipart: `file=@avatar.png` | `200 {"secureUrl":"https://.../avatar.png","publicId":"avatar_public_id","user":{"userID":10,"fullName":"QA Customer","email":"qa.customer@example.com","avatarURL":"https://.../avatar.png"}}` |
| `DELETE /api/user/profile/avatar`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"success":true,"message":"Avatar deleted successfully."}` |
| `GET /api/user/wallet`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"walletId":1,"balance":500000}` |
| `PUT /api/user/password`<br>Authenticated | `{"oldPassword":"Test@123","newPassword":"NewTest@123","confirmPassword":"NewTest@123"}` | `200 {"success":true,"message":"Password changed successfully."}` |

## 4. Admin user management - 9 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/admin/users?search=<keyword>&role=customer&status=active&page=1&pageSize=10`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `200 {"items":[{"userId":10,"fullName":"QA Customer","email":"qa.customer@example.com","role":"customer","status":"active"}],"page":1,"pageSize":10,"totalItems":1,"totalPages":1}` |
| `POST /api/admin/users`<br>Admin | `{"fullName":"QA Manager","email":"qa.manager@example.com","phone":"0901234568","password":"Test@123","role":"manager","status":"active","cinemaId":1}` | `201 {"userId":11,"fullName":"QA Manager","email":"qa.manager@example.com","phone":"0901234568","role":"manager","status":"active","cinemaId":1}` |
| `PUT /api/admin/users/<userId>`<br>Admin | `{"fullName":"QA Manager Updated","email":"qa.manager@example.com","phone":"0901234568","cinemaId":1}` | `200 {"userId":11,"fullName":"QA Manager Updated","email":"qa.manager@example.com","role":"manager","status":"active","cinemaId":1}` |
| `PATCH /api/admin/users/<userId>/role`<br>Admin | `{"role":"staff","cinemaId":1}` | `200 {"success":true,"message":"User role updated successfully."}` |
| `PATCH /api/admin/users/<userId>/status`<br>Admin | `{"status":"inactive"}` | `200 {"success":true,"message":"User status updated successfully."}` |
| `PATCH /api/admin/users/<userId>/password`<br>Admin | `{"password":"Reset@123","confirmPassword":"Reset@123"}` | `200 {"success":true,"message":"Password reset successfully."}` |
| `PUT /api/admin/users/<userId>/avatar`<br>Admin | Multipart: `file=@avatar.png` | `200 {"success":true,"message":"Avatar updated successfully.","avatarUrl":"https://.../avatar.png"}` |
| `DELETE /api/admin/users/<userId>/avatar`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `200 {"success":true,"message":"Avatar deleted successfully."}` |
| `DELETE /api/admin/users/<userId>`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |

## 5. Cinema - 5 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/cinemas`<br>Public | Body: none | `200 [{"cinemaId":1,"cinemaName":"QA Cinema","address":"01 Test Street","status":"active","createdAt":"2026-07-01T10:00:00+07:00","updatedAt":"2026-07-01T10:00:00+07:00"}]` |
| `GET /api/cinemas/<cinemaId>`<br>Public | Body: none | `200 {"cinemaId":1,"cinemaName":"QA Cinema","address":"01 Test Street","status":"active","createdAt":"2026-07-01T10:00:00+07:00","updatedAt":"2026-07-01T10:00:00+07:00"}` |
| `POST /api/cinemas`<br>Admin | `{"cinemaName":"QA Cinema","address":"01 Test Street","status":"active"}` | `201 {"cinemaId":1,"cinemaName":"QA Cinema","address":"01 Test Street","status":"active","createdAt":"2026-07-01T10:00:00+07:00","updatedAt":"2026-07-01T10:00:00+07:00"}` |
| `PUT /api/cinemas/<cinemaId>`<br>Admin | `{"cinemaName":"QA Cinema Updated","address":"02 Test Street","status":"active"}` | `200 {"cinemaId":1,"cinemaName":"QA Cinema Updated","address":"02 Test Street","status":"active","createdAt":"2026-07-01T10:00:00+07:00","updatedAt":"2026-07-01T11:00:00+07:00"}` |
| `DELETE /api/cinemas/<cinemaId>`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |

## 6. Room - 5 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/rooms`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 [{"roomId":1,"cinemaId":1,"name":"QA Room 01","type":"Standard","capacity":3,"status":"active","description":"QA room","createdAt":"2026-07-01T10:05:00+07:00"}]` |
| `GET /api/rooms/<roomId>`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"roomId":1,"cinemaId":1,"name":"QA Room 01","type":"Standard","capacity":3,"status":"active","description":"QA room","createdAt":"2026-07-01T10:05:00+07:00"}` |
| `POST /api/rooms`<br>Manager | `{"cinemaId":1,"name":"QA Room 01","type":"Standard","status":"active","description":"QA room"}` | `201 {"roomId":1,"cinemaId":1,"name":"QA Room 01","type":"Standard","capacity":0,"status":"active","description":"QA room","createdAt":"2026-07-01T10:05:00+07:00"}` |
| `PUT /api/rooms/<roomId>`<br>Manager | `{"cinemaId":1,"name":"QA Room Updated","type":"Standard","status":"active","description":"Updated"}` | `200 {"roomId":1,"cinemaId":1,"name":"QA Room Updated","type":"Standard","capacity":3,"status":"active","description":"Updated","createdAt":"2026-07-01T10:05:00+07:00"}` |
| `DELETE /api/rooms/<roomId>`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `204 No Content` |

## 7. Seat - 7 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/rooms/<roomId>/seats`<br>Public | Body: none | `200 [{"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"active"}]` |
| `GET /api/rooms/<roomId>/seats/<seatId>`<br>Public | Body: none | `200 {"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"active"}` |
| `POST /api/rooms/<roomId>/seats`<br>Manager | `{"rowLabel":"A","seatNumber":1,"seatTypeId":1,"status":"active"}` | `201 {"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"active"}` |
| `PATCH /api/rooms/<roomId>/seats/<seatId>`<br>Manager | `{"seatTypeId":1,"status":"inactive"}` | `200 {"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"inactive"}` |
| `DELETE /api/rooms/<roomId>/seats/<seatId>`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `204 No Content` |
| `GET /api/rooms/<roomId>/layout`<br>Public | Body: none | `200 {"roomId":1,"totalRows":2,"totalCols":3,"seats":[{"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"active"}]}` |
| `PUT /api/rooms/<roomId>/layout`<br>Manager | `{"totalRows":2,"totalCols":3,"seats":[{"rowLabel":"A","colIndex":1,"seatName":"A1","seatTypeId":1,"status":"active","isWalkway":false},{"rowLabel":"A","colIndex":2,"seatName":"A2","seatTypeId":1,"status":"active","isWalkway":false},{"rowLabel":"B","colIndex":1,"seatName":null,"seatTypeId":null,"status":null,"isWalkway":true}]}` | `200 {"roomId":1,"totalRows":2,"totalCols":3,"seats":[{"seatId":1,"roomId":1,"rowLabel":"A","seatNumber":1,"seatCode":"A1","seatTypeId":1,"type":"Standard","status":"active"},{"seatId":2,"roomId":1,"rowLabel":"A","seatNumber":2,"seatCode":"A2","seatTypeId":1,"type":"Standard","status":"active"}]}` |

## 8. Seat type - 5 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/seat-types`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `200 [{"seatTypeId":1,"typeName":"Standard","capacity":1,"extraPrice":0}]` |
| `GET /api/seat-types/<seatTypeId>`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `200 {"seatTypeId":1,"typeName":"Standard","capacity":1,"extraPrice":0}` |
| `POST /api/seat-types`<br>Manager | `{"typeName":"QA Standard","capacity":1,"extraPrice":0}` | `201 {"seatTypeId":1,"typeName":"QA Standard","capacity":1,"extraPrice":0}` |
| `PUT /api/seat-types/<seatTypeId>`<br>Manager | `{"typeName":"QA Standard Updated","capacity":1,"extraPrice":10000}` | `200 {"seatTypeId":1,"typeName":"QA Standard Updated","capacity":1,"extraPrice":10000}` |
| `DELETE /api/seat-types/<seatTypeId>`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `204 No Content` |

## 9. Genre - 5 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/genres`<br>Public | Body: none | `200 [{"genreId":1,"genreName":"Action"}]` |
| `GET /api/genres/1`<br>Public | Body: none | `200 {"genreId":1,"genreName":"Action"}` |
| `POST /api/genres`<br>Admin | `{"genreName":"QA Action"}` | `201 {"genreId":1,"genreName":"QA Action"}` |
| `PUT /api/genres/1`<br>Admin | `{"genreName":"QA Action Updated"}` | `200 {"genreId":1,"genreName":"QA Action Updated"}` |
| `DELETE /api/genres/1`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |

## 10. Movie - 7 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/movie?status=now_showing&genreId=1&genreName=Action&pageIndex=1&pageSize=10`<br>Public | Body: none | `200 {"items":[{"movieId":1,"title":"QA Movie","genres":["Action"],"ageRating":"T13","posterUrl":null,"durationMinutes":120,"status":"now_showing"}],"totalCount":1,"pageIndex":1,"pageSize":10}` |
| `GET /api/movie/<movieId>`<br>Public | Body: none | `200 {"movieId":1,"title":"QA Movie","genres":["Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","description":"QA movie","durationMinutes":120,"showingFromDate":"2026-06-30T00:00:00+07:00","showingToDate":"2026-07-31T00:00:00+07:00","posterUrl":null,"posterPublicId":null,"trailerUrl":null,"status":"now_showing"}` |
| `GET /api/movie/search?keyword=QA`<br>Public | Body: none | `200 [{"movieId":1,"title":"QA Movie","genres":["Action"],"ageRating":"T13","posterUrl":null,"durationMinutes":120,"status":"now_showing"}]` |
| `POST /api/movie`<br>Admin | `{"title":"QA Movie","genres":["QA Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","synopsis":"QA movie","durationMinutes":120,"showingFromDate":"2026-06-30","showingToDate":"2026-07-31","posterUrl":null,"posterPublicId":null,"trailerUrl":null}` | `201 {"movieId":1,"title":"QA Movie","genres":["QA Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","description":"QA movie","durationMinutes":120,"showingFromDate":"2026-06-30T00:00:00+07:00","showingToDate":"2026-07-31T00:00:00+07:00","posterUrl":null,"posterPublicId":null,"trailerUrl":null,"status":"now_showing"}` |
| `PUT /api/movie/<movieId>`<br>Admin | `{"title":"QA Movie Updated","genres":["QA Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","synopsis":"Updated","durationMinutes":120,"showingFromDate":"2026-06-30","showingToDate":"2026-07-31","posterUrl":null,"posterPublicId":null,"trailerUrl":null,"status":"now_showing"}` | `200 {"movieId":1,"title":"QA Movie Updated","genres":["QA Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","description":"Updated","durationMinutes":120,"showingFromDate":"2026-06-30T00:00:00+07:00","showingToDate":"2026-07-31T00:00:00+07:00","posterUrl":null,"posterPublicId":null,"trailerUrl":null,"status":"now_showing"}` |
| `PUT /api/movie/<movieId>/poster`<br>Admin | Multipart: `file=@poster.jpg` | `200 {"movieId":1,"title":"QA Movie Updated","genres":["QA Action"],"ageRating":"T13","director":"QA Director","cast":"Tester One","description":"Updated","durationMinutes":120,"showingFromDate":"2026-06-30T00:00:00+07:00","showingToDate":"2026-07-31T00:00:00+07:00","posterUrl":"https://.../poster.jpg","posterPublicId":"poster_public_id","trailerUrl":null,"status":"now_showing"}` |
| `DELETE /api/movie/<movieId>`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |

Ghi chú query params của `GET /api/movie`:

- `status`: giá trị hợp lệ theo movie status trong source.
- `genreId`: danh sách ID dạng phân tách bởi dấu phẩy, ví dụ `1,2,3`.
- `genreName`: danh sách tên genre dạng phân tách bởi dấu phẩy, ví dụ `Action,Drama`.
- `pageIndex` và `pageSize`: số nguyên dương.

## 11. Showtime - 6 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/showtimes?movieId=1&cinemaId=1&movieName=QA&roomName=QA Room&date=2026-07-05&status=scheduled&page=1&pageSize=10&sortBy=startTime&sortDir=asc`<br>Public | Body: none | `200 {"items":[{"showtimeId":1,"movie":{"movieId":1,"title":"QA Movie","ageRating":"T13","durationMin":120,"posterUrl":null},"room":{"roomId":1,"roomName":"QA Room","roomType":"Standard","capacity":3},"cinema":{"cinemaId":1,"cinemaName":"QA Cinema","address":"01 Test Street","status":"active"},"startTime":"2026-07-05T19:30:00+07:00","endTime":"2026-07-05T22:00:00+07:00","basePrice":90000,"status":"scheduled","isSoldOut":false}],"page":1,"pageSize":10,"totalItems":1,"totalPages":1}` |
| `POST /api/showtimes`<br>Manager | `{"movieId":1,"roomId":1,"startTime":"2026-07-05T19:30:00+07:00","basePrice":90000}` | `201 {"showtimeId":1,"movie":{"movieId":1,"title":"QA Movie","ageRating":"T13","durationMin":120,"posterUrl":null},"room":{"roomId":1,"roomName":"QA Room","roomType":"Standard","capacity":3},"startTime":"2026-07-05T19:30:00+07:00","endTime":"2026-07-05T22:00:00+07:00","basePrice":90000,"status":"scheduled","isSoldOut":false}` |
| `PUT /api/showtimes/<showtimeId>`<br>Manager | `{"movieId":1,"roomId":1,"startTime":"2026-07-05T20:00:00+07:00","basePrice":100000,"status":"scheduled"}` | `200 {"showtimeId":1,"movie":{"movieId":1,"title":"QA Movie","ageRating":"T13","durationMin":120,"posterUrl":null},"room":{"roomId":1,"roomName":"QA Room","roomType":"Standard","capacity":3},"startTime":"2026-07-05T20:00:00+07:00","endTime":"2026-07-05T22:30:00+07:00","basePrice":100000,"status":"scheduled","isSoldOut":false}` |
| `DELETE /api/showtimes/<showtimeId>`<br>Manager | Header: `Authorization: Bearer <managerToken>` | `204 No Content` |
| `GET /api/showtimes/<showtimeId>`<br>Public | Body: none | `200 {"showtimeID":1,"startTime":"2026-07-05T19:30:00+07:00","endTime":"2026-07-05T22:00:00+07:00","basePrice":90000,"status":"scheduled","movieID":1,"movieTitle":"QA Movie","posterURL":null,"durationMin":120,"ageRating":"T13","roomID":1,"roomName":"QA Room","roomType":"Standard","cinemaID":1,"cinemaName":"QA Cinema","cinemaAddress":"01 Test Street"}` |
| `GET /api/showtimes/<showtimeId>/seats`<br>Public | Body: none | `200 {"showtimeID":1,"roomName":"QA Room","roomType":"Standard","seats":[{"seatID":1,"seatRow":"A","seatCol":1,"seatType":"Standard","extraPrice":0,"price":90000,"status":"available"}]}` |

Quy tắc lịch chiếu hiện tại:

- `POST` tự tính `status` từ `startTime`; `PUT` chỉ đổi `status` khi request truyền giá trị mới.
- `PUT` có thể bỏ field `status`; khi bỏ thì giữ nguyên status hiện tại. Giá trị hợp lệ: `scheduled`, `cancelled`, `completed`.
- Trong cùng cinema và cùng `startTime`, mỗi `roomType` chỉ có tối đa một showtime không bị `cancelled`.
- Showtime `cancelled` không chặn việc tạo hoặc cập nhật showtime khác tại cùng thời điểm và loại phòng.

Ghi chú query params của `GET /api/showtimes`:

- `movieId`, `cinemaId`: số nguyên tùy chọn để lọc.
- `movieName`, `roomName`: chuỗi tùy chọn để lọc theo tên.
- `date`: `yyyy-MM-dd`.
- `status`: giá trị status showtime hợp lệ trong source.
- `page`, `pageSize`: số nguyên dương.
- `sortBy`: mặc định `startTime`.
- `sortDir`: mặc định `asc`.

## 12. Product/F&B - 7 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/products`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `200 {"products":[{"itemID":1,"itemName":"QA Combo","itemType":"combo","description":"Popcorn and drink","price":75000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-01T10:00:00+07:00"}]}` |
| `GET /api/products/available`<br>Public | Body: none | `200 {"products":[{"itemID":1,"itemName":"QA Combo","itemType":"combo","description":"Popcorn and drink","price":75000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-01T10:00:00+07:00"}]}` |
| `GET /api/products/<productId>`<br>Public | Body: none | `200 {"itemID":1,"itemName":"QA Combo","itemType":"combo","description":"Popcorn and drink","price":75000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-01T10:00:00+07:00"}` |
| `POST /api/products`<br>Admin | `{"itemName":"QA Combo","itemType":"combo","description":"Popcorn and drink","price":75000,"imageURL":null,"isLoyaltyEligible":true}` | `201 {"itemID":1,"itemName":"QA Combo","itemType":"combo","description":"Popcorn and drink","price":75000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-01T10:00:00+07:00"}` |
| `PUT /api/products/<productId>`<br>Admin | `{"itemName":"QA Combo Updated","itemType":"combo","description":"Updated","price":80000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock"}` | `200 {"itemID":1,"itemName":"QA Combo Updated","itemType":"combo","description":"Updated","price":80000,"imageURL":null,"isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-01T11:00:00+07:00"}` |
| `DELETE /api/products/<productId>`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |
| `PUT /api/products/<productId>/image`<br>Admin | Multipart: `file=@product.jpg` | `200 {"itemID":1,"itemName":"QA Combo Updated","itemType":"combo","description":"Updated","price":80000,"imageURL":"https://.../product.jpg","isLoyaltyEligible":true,"status":"in_stock","updatedAt":"2026-07-02T14:00:00+07:00"}` |

## 13. Booking và seat hold - 6 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `POST /api/seat-holds`<br>Customer/Staff | `{"showtimeId":1,"seatIds":[1,2]}` | `200 {"holdIds":[501,502],"expiresAt":"2026-07-01T17:20:00+07:00"}` |
| `DELETE /api/seat-holds`<br>Customer/Staff | `{"showtimeId":1,"seatIds":[1,2]}` | `200 {"success":true,"message":"Seat holds released successfully."}` |
| `POST /api/bookings/calculate-pricing`<br>Customer/Staff | `{"customerId":null,"showtimeId":1,"seatIds":[1,2],"fnbItems":[{"itemId":1,"quantity":2}],"voucherCode":null}` | `200 {"seatsSubTotal":180000,"fnBSubTotal":150000,"totalBeforeDiscount":330000,"membershipDiscount":0,"voucherDiscount":0,"totalDiscount":0,"finalAmount":330000,"seatDetails":[{"seatId":1,"seatRow":"A","seatCol":1,"seatTypeName":"Standard","price":90000}],"fnBDetails":[{"itemId":1,"itemName":"QA Combo","quantity":2,"unitPrice":75000,"subTotal":150000}],"voucherDetails":null}` |
| `POST /api/bookings`<br>Customer/Staff | `{"customerId":null,"showtimeId":1,"seatIds":[1,2],"fnbItems":[{"itemId":1,"quantity":2}],"voucherCode":null}` | `200 {"bookingID":1,"bookingCode":"BK202607010900001234","showtimeID":1,"movieTitle":"QA Movie","startTime":"2026-07-05T19:30:00+07:00","cinemaName":"QA Cinema","roomName":"QA Room","subTotal":330000,"discountAmount":0,"finalAmount":330000,"status":"pending","bookingDate":"2026-07-01T16:00:00+07:00","seats":[{"seatID":1,"seatRow":"A","seatCol":1,"ticketPrice":90000}],"fnbItems":[{"itemName":"QA Combo","quantity":2,"unitPrice":75000,"subTotal":150000}],"voucherApplied":null}` |
| `GET /api/bookings/<bookingId>`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"bookingID":1,"bookingCode":"BK202607010900001234","showtimeID":1,"movieTitle":"QA Movie","finalAmount":330000,"status":"pending","seats":[{"seatID":1,"seatRow":"A","seatCol":1,"ticketPrice":90000}],"fnbItems":[],"voucherApplied":null}` |
| `GET /api/bookings/my`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 [{"bookingID":1,"bookingCode":"BK202607010900001234","showtimeID":1,"movieTitle":"QA Movie","finalAmount":330000,"status":"pending","seats":[{"seatID":1,"seatRow":"A","seatCol":1,"ticketPrice":90000}]}]` |

## 14. Payment - 6 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `POST /api/payments/initiate`<br>Customer/Staff | `{"bookingId":1,"paymentMethod":"payos"}` | `200 {"success":true,"paymentId":1,"bookingId":1,"paymentMethod":"PAYOS","amount":330000,"status":"PENDING","checkoutUrl":"https://pay.payos.vn/web/...","qrCode":"<qrCodeData>","paymentLinkId":"<paymentLinkId>","orderCode":1751436000123456,"sessionId":1,"expiresAt":"2026-07-01T16:15:00+07:00"}` |
| `POST /api/payments/cash/confirm`<br>Staff | `{"paymentId":1}` | `200 {"paymentId":1,"bookingId":1,"paymentMethod":"CASH","amount":330000,"status":"SUCCESS","paidAt":"2026-07-01T16:10:00+07:00","createdAt":"2026-07-01T16:00:00+07:00"}` |
| `POST /api/payments/vnpay/callback?vnp_TxnRef=1&vnp_ResponseCode=00&vnp_TransactionNo=123&vnp_SecureHash=<hash>`<br>Public | Query do VNPay gửi; Body: none | `200 {"success":true,"message":"Payment completed successfully","paymentId":1,"bookingId":1,"paymentStatus":"SUCCESS","bookingStatus":"PAID"}` |
| `POST /api/payments/payos/webhook`<br>Public (PayOS) | `{"code":"00","desc":"success","success":true,"data":{"orderCode":1751436000123456,"amount":330000,"description":"PAY 1","accountNumber":"12345678","reference":"FT260701000001","transactionDateTime":"2026-07-01 16:05:00","currency":"VND","paymentLinkId":"<paymentLinkId>","code":"00","desc":"Thành công"},"signature":"<validPayOSSignature>"}` | `200 {"success":true,"message":"Payment completed successfully.","paymentId":1,"bookingId":1,"paymentStatus":"SUCCESS","bookingStatus":"PAID"}` |
| `GET /api/payments/<paymentId>`<br>Customer/Staff | Header: `Authorization: Bearer <token>` | `200 {"paymentId":1,"bookingId":1,"paymentMethod":"VNPAY","amount":330000,"status":"SUCCESS","paidAt":"2026-07-01T16:10:00+07:00","createdAt":"2026-07-01T16:00:00+07:00"}` |
| `GET /api/payments/booking/<bookingId>`<br>Customer/Staff | Header: `Authorization: Bearer <token>` | `200 {"paymentId":1,"bookingId":1,"paymentMethod":"VNPAY","amount":330000,"status":"SUCCESS","paidAt":"2026-07-01T16:10:00+07:00","createdAt":"2026-07-01T16:00:00+07:00"}` |

Ghi chú tham số của `POST /api/payments/initiate`:

- `bookingId`: bắt buộc, số nguyên.
- `paymentMethod`: bắt buộc, hiện source đang dùng các giá trị `payos`, `vnpay`, `wallet`, `cash`.
- `cash` chỉ dùng cho `Staff`.
- Giá trị `paymentMethod` sẽ được chuẩn hóa ở tầng service, nhưng tester nên gửi đúng chữ thường như ví dụ.

Ghi chú query/body của các API payment khác:

- `POST /api/payments/vnpay/callback`: query string do VNPay gửi, trong source đang đọc toàn bộ query params và xử lý theo bộ tham số của VNPay; tối thiểu cần `vnp_TxnRef`, `vnp_ResponseCode`, `vnp_TransactionNo`, `vnp_SecureHash`.
- `POST /api/payments/payos/webhook`: body bắt buộc có `code`, `desc`, `success`, `data`, `signature`.
- `data` của PayOS webhook có các field chính: `orderCode`, `amount`, `description`, `accountNumber`, `reference`, `transactionDateTime`, `currency`, `paymentLinkId`, `code`, `desc`; các field đối tác có thể gửi thêm vẫn được contract chấp nhận nếu có trong source model.
- `POST /api/payments/cash/confirm`: body bắt buộc có `paymentId`.

## 15. Invoice - 3 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/invoices/<invoiceId>`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"invoiceId":1,"bookingId":1,"invoiceCode":"INV-000001","totalAmount":330000,"taxAmount":0,"issuedAt":"2026-07-01T16:10:00+07:00"}` |
| `GET /api/invoices/booking/<bookingId>`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"invoiceId":1,"bookingId":1,"invoiceCode":"INV-000001","totalAmount":330000,"taxAmount":0,"issuedAt":"2026-07-01T16:10:00+07:00"}` |
| `GET /api/invoices/code/INV-000001`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"invoiceId":1,"bookingId":1,"invoiceCode":"INV-000001","totalAmount":330000,"taxAmount":0,"issuedAt":"2026-07-01T16:10:00+07:00"}` |

## 16. Membership - 3 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/membership/me`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 {"currentTier":"Silver","nextTier":"Gold","pointsToNextTier":670,"totalPoints":330,"totalSpent":330000,"discountPercent":5}` |
| `GET /api/membership/tiers`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 [{"tierID":1,"tierName":"Silver","minPoints":0,"discountRate":0.05}]` |
| `GET /api/membership/points-history`<br>Authenticated | Header: `Authorization: Bearer <token>` | `200 [{"pointsDelta":330,"transactionType":"earn","description":"Booking payment","createdAt":"2026-07-01T16:10:00+07:00"}]` |

## 17. Voucher - 4 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/vouchers?pageIndex=1&pageSize=10&searchKeyword=QA`<br>Public | Body: none | `200 {"items":[{"voucherId":1,"voucherCode":"QA10","category":"general","discountType":"percentage","discountValue":10,"minOrderValue":100000,"maxUses":100,"usedCount":0,"validFrom":"2026-07-01T00:00:00+07:00","validUntil":"2026-07-31T23:59:59+07:00","imageUrl":null,"description":"QA discount","isActive":true,"createdAt":"2026-07-01T10:00:00+07:00"}],"pageIndex":1,"pageSize":10,"totalItems":1,"totalPages":1}` |
| `POST /api/vouchers`<br>Admin | Multipart: `voucherCode=QA10&discountType=percentage&discountValue=10&validFrom=2026-07-01T00:00:00+07:00&validUntil=2026-07-31T23:59:59+07:00&image=@voucher.jpg` | `201 {"voucherId":1,"voucherCode":"QA10","category":"general","discountType":"percentage","discountValue":10,"minOrderValue":100000,"maxUses":100,"usedCount":0,"validFrom":"2026-07-01T00:00:00+07:00","validUntil":"2026-07-31T23:59:59+07:00","imageUrl":"https://.../voucher.jpg","description":"QA discount","isActive":true,"createdAt":"2026-07-01T10:00:00+07:00"}` |
| `PUT /api/vouchers/<voucherId>`<br>Admin | Multipart: `voucherCode=QA10&discountType=percentage&discountValue=15&validFrom=2026-07-01T00:00:00+07:00&validUntil=2026-07-31T23:59:59+07:00&image=@voucher.jpg` | `200 {"voucherId":1,"voucherCode":"QA10","category":"general","discountType":"percentage","discountValue":15,"minOrderValue":100000,"maxUses":100,"usedCount":0,"validFrom":"2026-07-01T00:00:00+07:00","validUntil":"2026-07-31T23:59:59+07:00","imageUrl":"https://.../voucher.jpg","description":"QA discount updated","isActive":true,"createdAt":"2026-07-01T10:00:00+07:00"}` |
| `DELETE /api/vouchers/<voucherId>`<br>Admin | Header: `Authorization: Bearer <adminToken>` | `204 No Content` |

Ghi chú query params của `GET /api/vouchers`:

- `pageIndex`, `pageSize`: số nguyên dương.
- `searchKeyword`: chuỗi tìm kiếm tùy chọn.

## 18. Ticket - 1 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/tickets/booking/<bookingId>`<br>Customer/Staff | Header: `Authorization: Bearer <token>` | `200 {"success":true,"tickets":[{"ticketID":1,"bookingSeatID":10,"qrCode":"<uuid>","status":"valid","checkedInAt":null,"checkedInByID":null}]}` |

## 19. Reports - 4 API

| API / Role | Request mẫu | Response thành công mẫu |
|---|---|---|
| `GET /api/v1/reports/revenue-summary?startDate=2026-07-01&endDate=2026-07-31&cinemaId=1`<br>Admin/Manager | Body: none | `200 {"grossRevenue":1000000,"ticketRevenue":700000,"fnbRevenue":300000,"discountAmount":50000,"bookingCount":12,"ticketsSold":40,"averageOrderValue":83333.33}` |
| `GET /api/v1/reports/movie-performance?startDate=2026-07-01&endDate=2026-07-31&searchMovie=QA&cinemaId=1`<br>Admin/Manager | Body: none | `200 [{"movieId":1,"title":"QA Movie","showtimeCount":5,"bookingCount":12,"ticketsSold":40,"occupancyRate":0.8,"revenue":1000000}]` |
| `GET /api/v1/reports/top-selling?startDate=2026-07-01&endDate=2026-07-31&cinemaId=1`<br>Admin/Manager | Body: none | `200 [{"itemId":1,"itemName":"QA Combo","quantitySold":40,"revenue":300000}]` |
| `GET /api/v1/reports/export?format=excel&reportType=revenue&startDate=2026-07-01&endDate=2026-07-31&cinemaId=1`<br>Admin/Manager | Body: none | `200 file download with Excel content and appropriate Content-Type` |

Ghi chú query params của `GET /api/v1/reports/*`:

- `startDate` và `endDate` bắt buộc, định dạng `yyyy-MM-dd`.
- `cinemaId`: số nguyên tùy chọn.
- `searchMovie`: chuỗi tùy chọn cho `movie-performance`.
- `format`: chỉ nhận `excel` hoặc `pdf`.
- `reportType`: chỉ nhận `revenue`, `fnb`, hoặc `occupancy`.

## 20. Error response Tester cần kiểm tra

Mỗi API phía trên chỉ trình bày một response thành công. Tester cần kiểm tra thêm các response lỗi tương ứng:

| Trường hợp | Expected response |
|---|---|
| Thiếu/sai token | `401 Unauthorized` |
| Sai role hoặc ngoài phạm vi cinema | `403 {"success":false,"message":"..."}` |
| ID không tồn tại | `404 {"success":false,"message":"... not found..."}` |
| Request/validation sai | `400` với lỗi field hoặc `{ "success":false,"message":"..." }` |
| Trùng/xung đột dữ liệu | `409 {"success":false,"message":"..."}` |
| Lỗi cổng thanh toán ngoài | `502 {"success":false,"message":"..."}` |
| Xóa thành công | `204 No Content`, body rỗng |

Các điểm response hiện còn gộp hoặc chưa đồng nhất trong source:

- Seat không tồn tại, sai room hoặc inactive đang dùng chung message.
- Seat-map gộp showtime không tồn tại và không khả dụng thành một message `404`.
- Một số controller trả trực tiếp `ModelState`, nên validation body không luôn có `success/message`.
- Seat đã được đặt hoặc đang được user khác giữ được map thành `409 Conflict`; seat không tồn tại, sai room hoặc inactive được map thành `400` kèm chi tiết theo nhóm.
- `DELETE /api/seat-holds` dùng cơ chế fail-fast: nếu có bất kỳ ghế nào không phải hold `holding` còn hiệu lực của người gọi thì toàn bộ request bị từ chối và không ghế nào được giải phóng.

## 18. Thứ tự test end-to-end đề xuất

1. Register/verify/login các tài khoản test.
2. Admin tạo cinema, manager và staff; gán đúng cinema.
3. Manager tạo seat type, room và layout.
4. Manager tạo genre, movie `now_showing`, showtime `scheduled`.
5. Manager tạo product còn hàng.
6. Customer lấy showtime/seat-map và giữ ghế.
7. Customer tính giá rồi tạo booking bằng đúng ghế đang giữ.
8. Customer/Staff khởi tạo payment; hoàn tất VNPay, PayOS, wallet hoặc cash. Với PayOS, webhook phải có chữ ký hợp lệ do PayOS gửi.
9. Kiểm tra booking, payment, invoice và membership.
10. Test concurrency bằng hai customer cùng giữ một ghế.
| `GET /api/showtimes?movieId=1&cinemaId=1&movieName=QA&roomName=QA Room&date=2026-07-05&status=scheduled&page=1&pageSize=10&sortBy=startTime&sortDir=asc`<br>Public | Body: none | `200 {"items":[{"showtimeId":1,"movie":{"movieId":1,"title":"QA Movie","ageRating":"T13","durationMin":120,"posterUrl":null},"room":{"roomId":1,"roomName":"QA Room","roomType":"Standard","capacity":3},"cinema":{"cinemaId":1,"cinemaName":"QA Cinema","address":"01 Test Street","status":"active"},"startTime":"2026-07-05T19:30:00+07:00","endTime":"2026-07-05T22:00:00+07:00","basePrice":90000,"status":"scheduled","isSoldOut":false}],"page":1,"pageSize":10,"totalItems":1,"totalPages":1}` |

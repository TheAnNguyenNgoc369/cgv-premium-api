# 🐛 BÁO CÁO BUGS - PRODUCT F&B CRUD

**Ngày kiểm tra**: 2026-06-30  
**Tester**: Claude (AI Tester)  
**Phạm vi**: CRUD cho Product/F&B system

---

## 🔴 BUG NGHIÊM TRỌNG (Phải fix ngay)

### Bug #1: Thiếu validation MaxLength - Có thể gây SQL Exception

**Mức độ**: CRITICAL  
**Vị trí**: 
- `ProductRequest.cs` (lines 6, 8, 11)
- `ProductService.cs` (lines 46-50, 82, 85, 115-119, 166, 169)

**Mô tả**:
Các trường string không có validation độ dài tối đa:
- `ItemName` - giới hạn DB: 150 chars (không validate)
- `Description` - giới hạn DB: 500 chars (không validate)
- `ImageURL` - giới hạn DB: 500 chars (không validate)

**Tác động**:
- Client có thể gửi string quá dài
- SQL Server sẽ throw exception thay vì trả về lỗi validation rõ ràng
- Bad user experience: lỗi kỹ thuật thay vì message thân thiện

**Cách tái hiện**:
```bash
POST /api/products
Content-Type: application/json
Authorization: Bearer {manager_token}

{
  "itemName": "AAAAAAAAAA..." (lặp lại 200 ký tự),
  "itemType": "snack",
  "description": "...",
  "price": 10000,
  "stockQuantity": 100,
  "isOnMenu": true,
  "isLoyaltyEligible": false
}
```

**Kết quả hiện tại**: SQL Exception (500 Internal Server Error)  
**Kết quả mong đợi**: 400 Bad Request với message "ItemName must not exceed 150 characters"

---

## 🟠 BUG QUAN TRỌNG (Nên fix sớm)

### Bug #2: Status có thể bị reset không mong muốn khi Update

**Mức độ**: MAJOR  
**Vị trí**: `ProductController.cs:100`

**Code có vấn đề**:
```csharp
request.Status ?? "in_stock"  // ❌ Nguy hiểm!
```

**Mô tả**:
Khi update product, nếu field `Status` không được gửi trong request (null), nó sẽ tự động fallback về "in_stock".

**Kịch bản lỗi**:
1. Sản phẩm hiện tại có `Status = "out_of_stock"`
2. Admin muốn update giá: gửi PUT request với body:
   ```json
   {
     "itemName": "Coca Cola",
     "itemType": "beverage",
     "price": 15000,  // chỉ muốn đổi giá
     "stockQuantity": 0,
     "isOnMenu": true,
     "isLoyaltyEligible": true
     // ❌ quên không gửi "status"
   }
   ```
3. Status tự động reset về "in_stock" ❌
4. Sản phẩm hiển thị là "có hàng" dù StockQuantity = 0

**Tác động**: Dữ liệu inconsistent, có thể cho khách đặt món không có hàng

---

### Bug #3: Thiếu validation ItemType/Status values ở DTO layer

**Mức độ**: MAJOR  
**Vị trí**: `ProductRequest.cs`

**Mô tả**:
- ItemType chỉ validate ở Service layer (line 58-61 ProductService.cs)
- Status chỉ validate ở Service layer (line 148-151 ProductService.cs)
- Không có validation attributes như `[RegularExpression]` hoặc custom validation ở DTO

**Tác động**:
- Nếu có code path nào bypass service layer, giá trị invalid có thể pass qua
- Validation chậm hơn (phải đến service layer mới biết lỗi)
- Model validation errors không rõ ràng

**Best practice bị vi phạm**: Validation nên ở cả DTO layer (fail fast) và Service layer (business logic)

---

## 🟡 BUG TRUNG BÌNH

### Bug #4: GetAvailableProducts không check StockQuantity

**Mức độ**: MEDIUM  
**Vị trí**: `ProductRepository.cs:29`

**Code hiện tại**:
```csharp
.Where(p => p.IsOnMenu && p.Status == "in_stock")
// ❌ Không check p.StockQuantity > 0
```

**Câu hỏi**: 
Một sản phẩm có:
- `IsOnMenu = true`
- `Status = "in_stock"`
- `StockQuantity = 0`

→ Có nên hiển thị trong danh sách "available" không?

**Đề xuất**: Nên thêm filter `.Where(p => p.StockQuantity > 0)` để đảm bảo chỉ hiển thị sản phẩm thực sự còn hàng

---

### Bug #5: NameExistsAsync là case-sensitive

**Mức độ**: MEDIUM  
**Vị trí**: `ProductRepository.cs:50`

**Mô tả**:
```csharp
.Where(p => p.ItemName == itemName)  // case-sensitive comparison
```

Kết quả:
- "Coca Cola" và "coca cola" được coi là 2 sản phẩm khác nhau ✅ Allowed
- "Coca Cola" và "COCA COLA" được coi là 2 sản phẩm khác nhau ✅ Allowed

**Câu hỏi**: Đây có phải hành vi mong muốn không?

**Đề xuất nếu muốn case-insensitive**:
```csharp
.Where(p => EF.Functions.Collate(p.ItemName, "SQL_Latin1_General_CP1_CI_AS") == 
            EF.Functions.Collate(itemName, "SQL_Latin1_General_CP1_CI_AS"))
```

---

### Bug #6: ProductResponse thiếu nhiều fields quan trọng

**Mức độ**: MEDIUM  
**Vị trí**: `ProductController.cs:137-147`

**Fields bị thiếu trong response**:
- `StockQuantity` - Admin cần biết còn bao nhiêu hàng
- `Status` - Admin cần biết trạng thái (in_stock/out_of_stock/inactive)
- `IsOnMenu` - Admin cần biết sản phẩm có đang hiển thị không
- `UpdatedAt` - Admin cần biết lần cuối cập nhật khi nào

**Tác động**:
- Admin phải gọi GET by ID để xem đầy đủ thông tin
- Không thể quản lý inventory từ danh sách products
- Poor UX cho admin dashboard

---

## 📋 CÂNH HỎI & VẤN ĐỀ THIẾT KẾ

### Question #1: GET /api/products/{id} là public endpoint

**Vị trí**: `ProductController.cs:38`  
**Hiện tại**: Không có `[Authorize]` attribute

**Câu hỏi**: 
- Có nên cho phép anonymous users xem chi tiết sản phẩm không?
- Có sản phẩm nào cần giữ bí mật (internal, test products) không?

**Rủi ro tiềm ẩn**: Information disclosure nếu có sản phẩm internal

---

### Question #2: Dùng chung DTO cho Create và Update

**Hiện tại**: `ProductRequest` dùng cho cả POST và PUT

**Vấn đề**:
- Status là optional (nullable) vì Create không cần
- Nhưng Update thì nên required để tránh Bug #2
- Gây confusion: field nào required cho operation nào?

**Đề xuất**: 
- Tách thành `CreateProductRequest` và `UpdateProductRequest`
- Hoặc dùng 2 validation groups khác nhau

---

### Question #3: Không có Optimistic Concurrency Control

**Vị trí**: `ProductRepository.cs:71-97` (UpdateAsync)

**Kịch bản race condition**:
1. User A đọc product (Price = 10000)
2. User B đọc product (Price = 10000)
3. User A update Price = 12000 ✅ Saved
4. User B update Price = 11000 ✅ Saved (overwrites A's change!)

**Tác động**: Last-write-wins, có thể mất data

**Đề xuất nếu cần**:
- Thêm `RowVersion` property vào Product entity
- Check concurrency token trước khi update

---

## 📊 TỔNG KẾT

**Tổng số bugs**: 6 bugs + 3 questions  
**Critical**: 1  
**Major**: 2  
**Medium**: 3  

**Độ ưu tiên fix**:
1. ✅ Bug #1 (MaxLength validation) - FIX NGAY
2. ✅ Bug #2 (Status reset issue) - FIX NGAY  
3. ✅ Bug #3 (DTO validation) - FIX SỚM
4. ⚠️ Bug #4, #5, #6 - FIX khi có thời gian
5. ❓ Questions - Cần quyết định thiết kế

---

## 🔧 ĐỀ XUẤT FIXES

Tôi có thể tạo pull request với các fixes sau:

### Fix #1: Thêm MaxLength validation
- Update `ProductRequest.cs` với `[MaxLength]` attributes
- Update `ProductService.cs` validation logic

### Fix #2: Tách Create và Update requests
- Tạo `CreateProductRequest.cs` (Status không có)
- Tạo `UpdateProductRequest.cs` (Status required)
- Update Controller để dùng đúng DTO

### Fix #3: Thêm custom validation attributes
- Tạo `[ValidItemType]` attribute
- Tạo `[ValidProductStatus]` attribute

### Fix #4-6: Improvements
- Update GetAvailableProductsAsync filter
- Update ProductResponse với đầy đủ fields
- Consider case-insensitive name check

**Bạn muốn tôi fix bugs nào trước?**

# Hướng dẫn hoàn thiện bài web RoomGo Hà Nội

## 1. Chuẩn bị môi trường

Cài Visual Studio 2022 (workload **ASP.NET and web development**) hoặc VS Code, .NET SDK 9 và SQL Server Express/LocalDB. Mở PowerShell trong thư mục `RoomGoHanoi` và chạy:

```powershell
dotnet restore
dotnet run
```

Nếu LocalDB không có, cài SQL Server Express rồi đổi chuỗi `RoomGo` trong `appsettings.json` thành:

```json
"Server=.\\SQLEXPRESS;Database=RoomGoHanoi;Trusted_Connection=True;TrustServerCertificate=True"
```

Lần chạy đầu, `EnsureCreated()` sẽ sinh database và dữ liệu mẫu. Khi bắt đầu chỉnh sửa model nghiêm túc, thay bằng EF Core Migration:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 2. Trình tự demo để bảo vệ

1. Vào trang chủ, thử tìm theo từ khóa, quận/huyện và giá.
2. Đăng ký tài khoản mới. Tài khoản mặc định là **Người thuê**.
3. Đăng nhập, vào **Xác thực chủ trọ**, nhập số điện thoại và OTP `123456`; đăng nhập lại.
4. Vào **Tin của tôi**, đăng một phòng mới. Tin có trạng thái `Pending`.
5. Đăng nhập Admin: `admin@roomgo.vn` / `Admin@123`, mở **Quản trị → Kiểm duyệt tin đăng**, phê duyệt tin.
6. Quay lại trang tìm kiếm và kiểm tra tin vừa duyệt đã xuất hiện. Mở chi tiết, dùng cửa sổ chat để thấy SignalR đẩy tin nhắn theo thời gian thực giữa các tab trình duyệt.
7. Vào Dashboard để trình bày thống kê và thử khóa/mở khóa một tài khoản.

## 3. Bổ sung các phần để đạt mức bài nộp hoàn chỉnh

### 3.1 Bảo mật và xác thực

- Chuyển `AppUser.PasswordHash` sang mật khẩu BCrypt hoặc ASP.NET Core Identity. Không dùng mật khẩu thô như bản demo.
- Đặt khóa kết nối, API SMS và cookie secret vào User Secrets / biến môi trường, không commit vào Git.
- Bổ sung xác nhận email, quên mật khẩu bằng token dùng một lần, giới hạn đăng nhập/OTP và audit log.
- Kiểm tra quyền ở mọi action: chỉ chủ sở hữu được sửa/xóa tin; chỉ Admin duyệt, khóa tài khoản và xử lý báo cáo.

### 3.2 Upload ảnh phòng

Thêm `List<IFormFile> files` vào action `Listings/Create`, kiểm tra MIME type `image/jpeg`, `image/png`, `image/webp`, giới hạn 5 MB mỗi ảnh, đặt tên `Guid.NewGuid()` và lưu URL vào `RoomImages`. Không dùng tên tệp do người dùng gửi. Hiển thị gallery ảnh trong `Home/Detail` với `alt` mô tả.

### 3.3 Bản đồ và Geospatial Search

Ở file `_Layout.cshtml`, thêm CSS/JS Leaflet và một `div` map trong `Listings/Create`. Khi click map, ghi `lat/lng` vào hai input. `HomeController.Search` hiện tính khoảng cách Haversine đúng cho dữ liệu nhỏ. Khi dữ liệu lớn, dùng SQL Server `geography` + spatial index hoặc truy vấn bounding-box trước rồi tính bán kính.

### 3.4 AJAX

Tạo các action JSON (ví dụ `POST /api/favorites/{id}` và `POST /api/reports`) có `[ValidateAntiForgeryToken]`, sau đó gọi `fetch()` từ nút Yêu thích/Báo cáo. Cập nhật giao diện từ JSON thay vì tải lại trang. Với search, tạo endpoint `GET /api/listings` trả JSON và cập nhật danh sách/map marker mỗi khi bộ lọc đổi.

### 3.5 Chat

SignalR hub hiện phục vụ trình diễn realtime. Trước khi nộp, ở `ChatHub` lấy `Context.UserIdentifier`, kiểm tra người gọi là chủ bài đăng hoặc người thuê tham gia cuộc trò chuyện, lưu `ChatMessage` vào database và không nhận trường `sender` từ client. Thêm danh sách hội thoại, trạng thái đã xem và phân trang lịch sử.

### 3.6 Chức năng còn lại trong báo cáo

- **Yêu thích:** tạo `FavoritesController` thêm/xóa `Favorite` theo `UserId` và `ListingId`, làm trang danh sách yêu thích.
- **Báo cáo vi phạm:** tạo form `ViolationReport`, admin đổi `ReportStatus`, lưu `ProcessedById` và quyết định ẩn/gỡ tin.
- **Hồ sơ/đổi mật khẩu:** `ProfileController` cập nhật họ tên, điện thoại, avatar; yêu cầu mật khẩu cũ khi đổi mật khẩu.
- **Thống kê/xuất báo cáo:** nhóm dữ liệu `Listings` theo tháng/quận/trạng thái; dùng Chart.js, và tạo CSV bằng `File()` cho admin.

## 4. Responsive, SEO và kiểm thử

- Kiểm tra tại 375 px, 768 px và desktop; dùng grid Bootstrap `col-md-*`, nút lớn và biểu mẫu dễ thao tác.
- Mỗi trang có title/description riêng, URL thân thiện, heading chỉ một `h1`, alt text ảnh; thêm `robots.txt`, `sitemap.xml`, canonical và Open Graph.
- Viết test ít nhất cho đăng ký trùng email, login sai, không có quyền admin, chủ trọ chưa OTP, tin chưa duyệt không xuất hiện, và filter bán kính.
- Dùng DevTools kiểm tra không có secret trong JavaScript, thử tamper `id` URL và upload file không phải ảnh.

## 5. Gợi ý phân chia nhóm

| Thành viên | Phần việc |
| --- | --- |
| 1 | Account, Identity/OTP, Profile, bảo mật |
| 2 | Listings, upload ảnh, search/map, yêu thích |
| 3 | Chat, báo cáo vi phạm, Dashboard/Admin, kiểm thử |

Mỗi người chuẩn bị một luồng từ giao diện → controller → model/database để minh chứng phần Client-side, Server-side, Validator, AJAX và ATTT trong Chương 4 của báo cáo.

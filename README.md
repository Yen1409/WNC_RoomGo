# RoomGo Hà Nội

## Khởi tạo CSDL SQL Server (bắt buộc)

1. Mở SQL Server Management Studio (SSMS), kết nối tới `localhost\\SQLEXPRESS` hoặc instance SQL Server của bạn.
2. Mở file `Database/CSDL.sql` trong dự án và nhấn **Execute**. File này tạo database `RoomGoHanoi`, đủ 7 bảng theo báo cáo và dữ liệu mẫu.
3. Trong `appsettings.json`, đảm bảo chuỗi kết nối có đúng instance SQL Server. Nếu dùng LocalDB, đổi `Server=.\\SQLEXPRESS` thành `Server=(localdb)\\MSSQLLocalDB`.
4. Chạy `dotnet restore` rồi `dotnet run`.

Các model Entity Framework đã ánh xạ trực tiếp vào `tblUsers`, `tblTinDang`, `tblAnhPhong`, `tblPhongYeuThich`, `tblTinNhan`, `tblBCViPham` và `tblBCThongKe` của file SQL.

Website ASP.NET Core MVC cho đề tài **Hệ thống Ngân hàng Nhà trọ và Tìm kiếm Phòng trọ Thông minh tại Hà Nội**.

## Chức năng đã có

- Đăng ký, đăng nhập, phân quyền Người thuê / Chủ trọ / Admin.
- Xác thực OTP demo (`123456`) để nâng cấp thành Chủ trọ.
- Chủ trọ đăng và quản lý tin; trạng thái chờ duyệt, duyệt, từ chối, ẩn.
- Tìm kiếm theo từ khóa, quận, giá và tọa độ/bán kính (Geospatial Search theo công thức Haversine).
- Chi tiết phòng, kênh chat SignalR theo từng tin.
- Dashboard Admin, duyệt/từ chối tin và khóa/mở khóa tài khoản.
- Các biểu mẫu có kiểm tra dữ liệu server-side, Anti-forgery token và authorization bằng Cookie.

## Chạy từ đầu (Windows)

1. Cài **.NET SDK 9**, **SQL Server Express** (Database Engine) và SSMS. Mở Terminal tại thư mục này.
2. Khôi phục gói và chạy:

   ```powershell
   dotnet restore
   dotnet run
   ```

3. Mở địa chỉ mà terminal hiển thị (thường là `https://localhost:xxxx`). Lần chạy đầu tự tạo cơ sở dữ liệu SQL Server `RoomGoHanoi` và dữ liệu mẫu.
4. Tài khoản quản trị: `admin@roomgo.vn` / `Admin@123`. Tài khoản chủ trọ: `chutro@roomgo.vn` / `Owner@123`.

## Hoàn thiện trước khi nộp bài

1. **Mật khẩu:** thay so sánh chuỗi minh họa bằng ASP.NET Core Identity hoặc BCrypt (`BCrypt.Net-Next`). Không để mật khẩu thô trong cơ sở dữ liệu.
2. **OTP:** dùng SMS provider, lưu OTP dạng hash, thời hạn 5 phút, giới hạn số lần thử và rate-limit.
3. **Upload ảnh:** thêm `IFormFile`, chỉ cho JPEG/PNG/WebP, giới hạn dung lượng, đổi tên ngẫu nhiên, lưu ngoài web root hoặc object storage.
4. **Bản đồ thật:** thêm Leaflet và OpenStreetMap; chọn marker khi đăng tin, truyền tọa độ vào form; dùng bounding-box ở API khi dữ liệu lớn.
5. **Chat thật:** trong `ChatHub`, xác thực người dùng, lưu `ChatMessage` vào database, chỉ cho chủ tin và người thuê tham gia room; không tin `sender` từ JavaScript.
6. **AJAX:** tạo endpoint JSON cho tìm kiếm/lưu yêu thích/báo cáo, gọi bằng `fetch`, trả validation errors theo JSON để cập nhật danh sách không tải lại trang.
7. **CSDL/Migration:** khi thay model, dùng `dotnet ef migrations add TenMigration` rồi `dotnet ef database update`; production thay LocalDB bằng connection string SQL Server có secret.
8. **SEO/Responsive:** bổ sung sitemap.xml, robots.txt, canonical URL, Open Graph và ảnh `alt`; kiểm tra giao diện tại 375px, 768px và desktop.
9. **Kiểm thử:** kiểm thử luồng đăng ký → OTP → đăng tin → admin duyệt → tìm kiếm → chat; thử truy cập URL admin khi là người thuê để chứng minh phân quyền.

## Cấu trúc

- `Models/`: thực thể tương ứng các bảng Users, TinDang, AnhPhong, Yeuthich, TinNhan, BaoCao.
- `Data/`: EF Core context và seed dữ liệu.
- `Controllers/`: MVC cho trang công khai, tài khoản, chủ trọ, quản trị.
- `Hubs/`: SignalR Real-time Chat.
- `Views/`: Razor responsive UI.

> Lưu ý: đây là bản học phần có sẵn luồng hoàn chỉnh để demo. Các mục “Hoàn thiện trước khi nộp bài” là bắt buộc để đạt mức an toàn/thực tế cao hơn.

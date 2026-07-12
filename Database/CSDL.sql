
IF DB_ID(N'db_RoomGoWNC') IS NULL CREATE DATABASE db_RoomGoWNC;
GO
USE db_RoomGoWNC;
GO

-- 1. Tạo bảng tblUsers (Người dùng)
CREATE TABLE tblUsers (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    sHoten NVARCHAR(100) NOT NULL,
    sEmail VARCHAR(100) NOT NULL UNIQUE,
    sMatkhau VARCHAR(255) NOT NULL,
    sSdt VARCHAR(15),
    sVaitro NVARCHAR(20) NOT NULL, -- Người thuê/Chủ trọ/Admin
    sTrangthai NVARCHAR(20) DEFAULT N'Chờ duyệt',
    bDaXacThuc BIT DEFAULT 0,
    sAvatar VARCHAR(255),
    sDiachi NVARCHAR(255),
    dNgaydangky DATETIME DEFAULT GETDATE(),
);

-- 2. Tạo bảng tblTinDang (Tin đăng)
CREATE TABLE tblTinDang (
    tindang_id INT IDENTITY(1,1) PRIMARY KEY,
    fk_userID INT NOT NULL,
	fk_adminDuyetID INT,
    sTieude NVARCHAR(200) NOT NULL,
    sDiachi NVARCHAR(255) NOT NULL,
    sQuanhuyen NVARCHAR(100) NOT NULL,
    fGiaThue DECIMAL(18,0) NOT NULL,
    fDientich FLOAT NOT NULL,
    sMota NVARCHAR(MAX),
    sTienich NVARCHAR(MAX),
    fVido DECIMAL(10,7),
    fKinhdo DECIMAL(10,7),
    sTrangthaiTin NVARCHAR(20) DEFAULT N'Chờ duyệt', -- Chờ duyệt/Đã duyệt/Từ chối
    sLydoTuchoi NVARCHAR(255),
    dNgaydang DATETIME DEFAULT GETDATE(),
    dNgayduyet DATETIME,
    
    CONSTRAINT FK_tblTinDang_Users FOREIGN KEY (fk_userID) REFERENCES tblUsers(user_id),
    CONSTRAINT FK_tblTinDang_Admin FOREIGN KEY (fk_adminDuyetID) REFERENCES tblUsers(user_id)
);

-- 3. Tạo bảng tblAnhPhong (Hình ảnh phòng)
CREATE TABLE tblAnhPhong (
    anh_id INT IDENTITY(1,1) PRIMARY KEY,
    fk_tindangID INT NOT NULL,
    sHinhanh VARCHAR(255) NOT NULL,
    sMota NVARCHAR(255),
    dNgaytailen DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_tblAnhPhong_TinDang FOREIGN KEY (fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE
);

-- 4. Tạo bảng tblPhongYeuThich (Phòng yêu thích)
CREATE TABLE tblPhongYeuThich (
    yeuthich_id INT IDENTITY(1,1) PRIMARY KEY,
    fk_userID INT NOT NULL,
    fk_tindangID INT NOT NULL,
    dNgayluu DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_tblPhongYeuThich_Users FOREIGN KEY (fk_userID) REFERENCES tblUsers(user_id),
    CONSTRAINT FK_tblPhongYeuThich_TinDang FOREIGN KEY (fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE
);

-- 5. Tạo bảng tblTinNhan (Tin nhắn)
CREATE TABLE tblTinNhan (
    tinnhan_id INT IDENTITY(1,1) PRIMARY KEY,
    fk_nguoiGuiID INT NOT NULL,
    fk_nguoiNhanID INT NOT NULL,
    fk_tindangID INT,
    sNoidung NVARCHAR(MAX) NOT NULL,
    dThoigianGui DATETIME DEFAULT GETDATE(),
    bDaxem BIT DEFAULT 0,
    
    CONSTRAINT FK_tblTinNhan_NguoiGui FOREIGN KEY (fk_nguoiGuiID) REFERENCES tblUsers(user_id),
    CONSTRAINT FK_tblTinNhan_NguoiNhan FOREIGN KEY (fk_nguoiNhanID) REFERENCES tblUsers(user_id),
    CONSTRAINT FK_tblTinNhan_TinDang FOREIGN KEY (fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE SET NULL
);

-- 6. Tạo bảng tblBCViPham (Báo cáo vi phạm)
CREATE TABLE tblBCViPham (
    baocaoVP_id INT IDENTITY(1,1) PRIMARY KEY,
    fk_tindangID INT NOT NULL,
    fk_nguoiBaocaoID INT NOT NULL,
    fk_adminXulyID INT,
    sLydo NVARCHAR(255) NOT NULL,
    sNoidung NVARCHAR(MAX),
    dNgayBaocao DATETIME DEFAULT GETDATE(),
    sTrangthaiXuly NVARCHAR(20) DEFAULT N'Chưa xử lý',
    dNgayXuly DATETIME,
    
    CONSTRAINT FK_tblBCViPham_TinDang FOREIGN KEY (fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE,
    CONSTRAINT FK_tblBCViPham_NguoiBaocao FOREIGN KEY (fk_nguoiBaocaoID) REFERENCES tblUsers(user_id),
    CONSTRAINT FK_tblBCViPham_AdminXuly FOREIGN KEY (fk_adminXulyID) REFERENCES tblUsers(user_id)
);

-- 7. Tạo bảng tblBCThongKe (Báo cáo thống kê)
CREATE TABLE tblBCThongKe (
    baocaoTK_id INT IDENTITY(1,1) PRIMARY KEY,
    sTieude NVARCHAR(200) NOT NULL,
    sLoaiThongke NVARCHAR(20),
    dTungay DATE,
    dDenngay DATE,
    dNgaylap DATETIME DEFAULT GETDATE(),
    fk_adminLapID INT NOT NULL,
    iTongNguoidung INT DEFAULT 0,
    iTongChutro INT DEFAULT 0,
    iTongTindang INT DEFAULT 0,
    iTongTinDaduyet INT DEFAULT 0,
    iTongTinTuchoi INT DEFAULT 0,
    iTongBaocaoVipham INT DEFAULT 0,
    sTepBaocao VARCHAR(255),
    
    CONSTRAINT FK_tblBCThongKe_AdminLap FOREIGN KEY (fk_adminLapID) REFERENCES tblUsers(user_id)
);


-- ====================================================================
-- 1. CHÈN DỮ LIỆU MẪU CHO tblUsers
-- Gồm 1 Admin (id=1), 1 Chủ trọ (id=2), 1 Người thuê (id=3)
-- ====================================================================
INSERT INTO tblUsers (sHoten, sEmail, sMatkhau, sSdt, sVaitro, sTrangthai, bDaXacThuc, sAvatar, sDiachi)
VALUES 
(N'Nguyễn Thị Yến', 'admin@RoomGo.com', 'hashed_password_root_123', '0911222333', N'Admin', N'Hoạt động', 1, 'avatar_admin.jpg', N'Phố Huế, Hai Bà Trưng, Hà Nội'),
(N'Trần Thị Thảo', 'thao123@gmail.com', 'hashed_password_host_456', '0944555666', N'Chủ trọ', N'Hoạt động', 1, 'avatar_thao.jpg', N'Đường Cầu Giấy, Cầu Giấy, Hà Nội'),
(N'Nguyễn Văn A', 'nva@gmail.com', 'hashed_password_user_789', '', N'Người thuê', N'Hoạt động', 0, 'avatar_a.jpg', N'Thị xã Từ Sơn, Bắc Ninh');


-- ====================================================================
-- 2. CHÈN DỮ LIỆU MẪU CHO tblTinDang
-- Cả 3 tin đều do Chủ trọ (fk_userID = 2) đăng. 
-- Tin 1: Đã duyệt bởi Admin (1), Tin 2: Chờ duyệt, Tin 3: Bị từ chối bởi Admin (1)
-- ====================================================================
INSERT INTO tblTinDang (fk_userID, sTieude, sDiachi, sQuanhuyen, fGiaThue, fDientich, sMota, sTienich, fVido, fKinhdo, sTrangthaiTin, sLydoTuchoi, dNgayduyet, fk_adminDuyetID)
VALUES 
(2, N'Phòng trọ khép kín full nội thất gần ĐH Quốc Gia', N'Số 15, Ngõ 20, Đường Hồ Tùng Mậu', N'Cầu Giấy', 3500000, 25, N'Phòng rộng rãi, có điều hòa, nóng lạnh, giường tủ mới tinh. Giờ giấc tự do, không chung chủ.', N'Điều hòa, Nóng lạnh, Máy giặt chung, Wifi, Khóa vân tay', 21.0364, 105.7825, N'Đã duyệt', NULL, GETDATE(), 1),
(2, N'Căn hộ mini ban công thoáng mát, an ninh tốt', N'Số 8, Ngách 3, Ngõ 105, Doãn Kế Thiện', N'Cầu Giấy', 4200000, 30, N'Căn hộ tầng 3, ban công rộng phơi đồ thoải mái. Điện nước giá dân, an ninh đảm bảo 24/7.', N'Ban công, Điều hòa, Nóng lạnh, Tủ lạnh', 21.0395, 105.7791, N'Chờ duyệt', NULL, NULL, NULL),
(2, N'Nhà nguyên căn giá rẻ cho sinh viên ở ghép', N'Số 45, Đường Trần Thái Tông', N'Cầu Giấy', 8000000, 60, N'Nhà 2 tầng, 2 phòng ngủ, phù hợp nhóm bạn 4-6 người ở thoải mái. Cọc 1 tháng.', N'Chỗ để xe rộng, Bếp riêng', 21.0282, 105.7914, N'Từ chối', N'Tiêu đề viết hoa toàn bộ hoặc chứa ký tự không hợp lệ / Nội dung chưa rõ ràng', NULL, 1);


-- ====================================================================
-- 3. CHÈN DỮ LIỆU MẪU CHO tblAnhPhong
-- Ảnh phòng liên kết với Tin đăng 1 và 2 (fk_tindangID = 1, 2)
-- ====================================================================
INSERT INTO tblAnhPhong (fk_tindangID, sHinhanh, sMota)
VALUES 
(1, 'room1_giuong_tu.jpg', N'Góc giường ngủ và tủ quần áo'),
(1, 'room1_wc.jpg', N'Nhà vệ sinh khép kín sạch sẽ'),
(2, 'room2_bancong.jpg', N'Góc ban công phơi đồ hướng Đông Nam');


-- ====================================================================
-- 4. CHÈN DỮ LIỆU MẪU CHO tblPhongYeuThich
-- Người thuê (fk_userID = 3) lưu các tin đăng 1 và 2
-- ====================================================================
INSERT INTO tblPhongYeuThich (fk_userID, fk_tindangID)
VALUES 
(3, 1),
(3, 2);


-- ====================================================================
-- 5. CHÈN DỮ LIỆU MẪU CHO tblTinNhan
-- Cuộc hội thoại qua lại giữa Người thuê (3) và Chủ trọ (2) về Tin đăng (1)
-- ====================================================================
INSERT INTO tblTinNhan (fk_nguoiGuiID, fk_nguoiNhanID, fk_tindangID, sNoidung, bDaxem)
VALUES 
(3, 2, 1, N'Dạ chào chính chủ ạ, phòng trọ Hồ Tùng Mậu mình còn không ạ? Chiều nay em qua xem được không?', 1),
(2, 3, 1, N'Chào em, phòng vẫn còn nhé. Tầm 5h chiều em qua gọi số anh ra đón vào xem phòng.', 1),
(3, 2, 1, N'Vâng thế tầm 5h15 em tới đầu ngõ em gọi anh nhé, em cảm ơn ạ!', 0);


-- ====================================================================
-- 6. CHÈN DỮ LIỆU MẪU CHO tblBCViPham
-- Báo cáo vi phạm liên quan đến Tin đăng 3 (bị từ chối) hoặc các tin khác
-- ====================================================================
INSERT INTO tblBCViPham (fk_tindangID, fk_nguoiBaocaoID, fk_adminXulyID, sLydo, sNoidung, sTrangthaiXuly, dNgayXuly)
VALUES 
(3, 3, 1, N'Sai lệch thông tin giá cả', N'Chủ nhà đăng giá 8 triệu nhưng gọi điện lại báo giá thực tế là 10 triệu chưa điện nước.', N'Đã xử lý', GETDATE()),
(1, 3, NULL, N'Trùng lặp tin đăng', N'Tin này bị đăng lặp lại nhiều lần trong ngày gây loãng trang tìm kiếm.', N'Chưa xử lý', NULL);


-- ====================================================================
-- 7. CHÈN DỮ LIỆU MẪU CHO tblBCThongKe
-- Các báo cáo thống kê do Admin (fk_adminLapID = 1) 
-- ====================================================================
INSERT INTO tblBCThongKe (sTieude, sLoaiThongke, dTungay, dDenngay, fk_adminLapID, iTongNguoidung, iTongChutro, iTongTindang, iTongTinDaduyet, iTongTinTuchoi, iTongBaocaoVipham, sTepBaocao)
VALUES 
(N'Báo cáo tổng quan tình hình tháng 05/2026', N'Thống kê tháng', '2026-05-01', '2026-05-31', 1, 150, 45, 320, 280, 40, 12, 'bc_thang_5_2026.xlsx'),
(N'Báo cáo kiểm duyệt tin đăng Quý 2/2026', N'Thống kê quý', '2026-04-01', '2026-06-30', 1, 450, 120, 980, 850, 130, 45, 'bc_quy_2_2026.xlsx'),
(N'Thống kê tốc độ tăng trưởng người dùng năm 2026', N'Thống kê năm', '2026-01-01', '2026-12-31', 1, 1800, 520, 4500, 4100, 400, 115, 'bc_nam_2026.pdf');

SELECT * FROM tblUsers;
SELECT * FROM tblTinDang;
SELECT * FROM tblAnhPhong;
SELECT * FROM tblPhongYeuThich;
SELECT * FROM tblTinNhan;
SELECT * FROM tblBCViPham;
SELECT * FROM tblBCThongKe;
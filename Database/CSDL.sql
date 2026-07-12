
IF DB_ID(N'RoomGoHanoi') IS NULL CREATE DATABASE RoomGoHanoi;
GO
USE RoomGoHanoi;
GO

CREATE TABLE tblUsers (
 user_id INT IDENTITY(1,1) PRIMARY KEY, sHoten NVARCHAR(100) NOT NULL, sEmail VARCHAR(100) NOT NULL UNIQUE,
 sMatkhau VARCHAR(255) NOT NULL, sSdt VARCHAR(15) NULL, sVaitro NVARCHAR(20) NOT NULL,
 sTrangthai NVARCHAR(20) NOT NULL DEFAULT N'Hoạt động', bDaXacThuc BIT NOT NULL DEFAULT 0, bKhoa BIT NOT NULL DEFAULT 0,
 sAvatar VARCHAR(255) NULL, sDiachi NVARCHAR(255) NULL, dNgaydangky DATETIME NOT NULL DEFAULT GETDATE()
);
CREATE TABLE tblTinDang (
 tindang_id INT IDENTITY(1,1) PRIMARY KEY, fk_userID INT NOT NULL, fk_adminDuyetID INT NULL,
 sTieude NVARCHAR(200) NOT NULL, sDiachi NVARCHAR(255) NOT NULL, sQuanhuyen NVARCHAR(100) NOT NULL,
 fGiaThue DECIMAL(18,0) NOT NULL, fDientich FLOAT NOT NULL, sMota NVARCHAR(MAX) NOT NULL, sTienich NVARCHAR(MAX) NOT NULL,
 sAnhDaiDien VARCHAR(1000) NOT NULL, fVido DECIMAL(10,7) NULL, fKinhdo DECIMAL(10,7) NULL,
 sTrangthaiTin NVARCHAR(20) NOT NULL DEFAULT N'Pending', sLydoTuchoi NVARCHAR(255) NULL,
 dNgaydang DATETIME NOT NULL DEFAULT GETDATE(), dNgayduyet DATETIME NULL,
 CONSTRAINT FK_TinDang_User FOREIGN KEY(fk_userID) REFERENCES tblUsers(user_id),
 CONSTRAINT FK_TinDang_Admin FOREIGN KEY(fk_adminDuyetID) REFERENCES tblUsers(user_id)
);
CREATE TABLE tblAnhPhong (
 anh_id INT IDENTITY(1,1) PRIMARY KEY, fk_tindangID INT NOT NULL, sHinhanh VARCHAR(1000) NOT NULL,
 sMota NVARCHAR(255) NULL, dNgaytailen DATETIME NOT NULL DEFAULT GETDATE(),
 CONSTRAINT FK_AnhPhong_TinDang FOREIGN KEY(fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE
);
CREATE TABLE tblPhongYeuThich (
 yeuthich_id INT IDENTITY(1,1) PRIMARY KEY, fk_userID INT NOT NULL, fk_tindangID INT NOT NULL, dNgayluu DATETIME NOT NULL DEFAULT GETDATE(),
 CONSTRAINT UQ_PhongYeuThich UNIQUE(fk_userID,fk_tindangID),
 CONSTRAINT FK_YeuThich_User FOREIGN KEY(fk_userID) REFERENCES tblUsers(user_id),
 CONSTRAINT FK_YeuThich_TinDang FOREIGN KEY(fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE
);
CREATE TABLE tblTinNhan (
 tinnhan_id INT IDENTITY(1,1) PRIMARY KEY, fk_nguoiGuiID INT NOT NULL, fk_nguoiNhanID INT NOT NULL, fk_tindangID INT NULL,
 sNoidung NVARCHAR(MAX) NOT NULL, dThoigianGui DATETIME NOT NULL DEFAULT GETDATE(), bDaxem BIT NOT NULL DEFAULT 0,
 CONSTRAINT FK_TinNhan_Gui FOREIGN KEY(fk_nguoiGuiID) REFERENCES tblUsers(user_id),
 CONSTRAINT FK_TinNhan_Nhan FOREIGN KEY(fk_nguoiNhanID) REFERENCES tblUsers(user_id),
 CONSTRAINT FK_TinNhan_TinDang FOREIGN KEY(fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE SET NULL
);
CREATE TABLE tblBCViPham (
 baocaoVP_id INT IDENTITY(1,1) PRIMARY KEY, fk_tindangID INT NOT NULL, fk_nguoiBaocaoID INT NOT NULL, fk_adminXulyID INT NULL,
 sLydo NVARCHAR(255) NOT NULL, sNoidung NVARCHAR(MAX) NULL, dNgayBaocao DATETIME NOT NULL DEFAULT GETDATE(),
 sTrangthaiXuly NVARCHAR(20) NOT NULL DEFAULT N'Pending', dNgayXuly DATETIME NULL,
 CONSTRAINT FK_ViPham_TinDang FOREIGN KEY(fk_tindangID) REFERENCES tblTinDang(tindang_id) ON DELETE CASCADE,
 CONSTRAINT FK_ViPham_NguoiBaoCao FOREIGN KEY(fk_nguoiBaocaoID) REFERENCES tblUsers(user_id),
 CONSTRAINT FK_ViPham_Admin FOREIGN KEY(fk_adminXulyID) REFERENCES tblUsers(user_id)
);
CREATE TABLE tblBCThongKe (
 baocaoTK_id INT IDENTITY(1,1) PRIMARY KEY, sTieude NVARCHAR(200) NOT NULL, sLoaiThongke NVARCHAR(20) NULL,
 dTungay DATE NULL, dDenngay DATE NULL, dNgaylap DATETIME NOT NULL DEFAULT GETDATE(), fk_adminLapID INT NOT NULL,
 iTongNguoidung INT NOT NULL DEFAULT 0, iTongChutro INT NOT NULL DEFAULT 0, iTongTindang INT NOT NULL DEFAULT 0,
 iTongTinDaduyet INT NOT NULL DEFAULT 0, iTongTinTuchoi INT NOT NULL DEFAULT 0, iTongBaocaoVipham INT NOT NULL DEFAULT 0, sTepBaocao VARCHAR(255) NULL,
 CONSTRAINT FK_ThongKe_Admin FOREIGN KEY(fk_adminLapID) REFERENCES tblUsers(user_id)
);
GO

INSERT INTO tblUsers(sHoten,sEmail,sMatkhau,sSdt,sVaitro,sTrangthai,bDaXacThuc,bKhoa,sDiachi) VALUES
(N'Nguyễn Thị Yến','admin@roomgo.vn','Admin@123','0911222333',N'Admin',N'Hoạt động',1,0,N'Phố Huế, Hai Bà Trưng, Hà Nội'),
(N'Trần Thị Thảo','chutro@roomgo.vn','Owner@123','0944555666',N'Owner',N'Hoạt động',1,0,N'Đường Cầu Giấy, Hà Nội'),
(N'Nguyễn Văn A','tenant@roomgo.vn','Tenant@123','',N'Tenant',N'Hoạt động',0,0,N'Long Biên, Hà Nội');
INSERT INTO tblTinDang(fk_userID,fk_adminDuyetID,sTieude,sDiachi,sQuanhuyen,fGiaThue,fDientich,sMota,sTienich,sAnhDaiDien,fVido,fKinhdo,sTrangthaiTin,dNgayduyet) VALUES
(2,1,N'Phòng trọ khép kín full nội thất gần ĐH Quốc Gia',N'Số 15 ngõ 20 đường Hồ Tùng Mậu',N'Cầu Giấy',3500000,25,N'Phòng rộng rãi, có điều hòa, nóng lạnh, giường tủ mới, giờ giấc tự do và không chung chủ.',N'Điều hòa, nóng lạnh, máy giặt, Wifi, khóa vân tay','https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=900&q=85',21.0364,105.7825,N'Approved',GETDATE()),
(2,NULL,N'Căn hộ mini ban công thoáng mát',N'Số 8 ngách 3 ngõ 105 Doãn Kế Thiện',N'Cầu Giấy',4200000,30,N'Căn hộ tầng 3, ban công rộng, điện nước giá dân, an ninh 24/7.',N'Ban công, điều hòa, nóng lạnh, tủ lạnh','https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=900&q=85',21.0395,105.7791,N'Pending',NULL),
(2,1,N'Nhà nguyên căn cho sinh viên ở ghép',N'Số 45 đường Trần Thái Tông',N'Cầu Giấy',8000000,60,N'Nhà hai tầng, hai phòng ngủ, phù hợp nhóm bạn.',N'Chỗ để xe rộng, bếp riêng','https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=900&q=85',21.0282,105.7914,N'Rejected',NULL);
INSERT INTO tblTinDang(fk_userID,fk_adminDuyetID,sTieude,sDiachi,sQuanhuyen,fGiaThue,fDientich,sMota,sTienich,sAnhDaiDien,fVido,fKinhdo,sTrangthaiTin,dNgayduyet) VALUES
(2,1,N'Studio gần Đại học Bách Khoa',N'45 Đại Cồ Việt',N'Hai Bà Trưng',4200000,28,N'Phòng mới, sáng, an ninh và giờ giấc tự do.',N'Điều hòa, nóng lạnh, thang máy','https://images.unsplash.com/photo-1560185007-cde436f6a4d0?auto=format&fit=crop&w=900&q=85',21.005,105.843,N'Approved',GETDATE()),
(2,1,N'Phòng yên tĩnh gần Nguyễn Trãi',N'96 Nguyễn Trãi',N'Thanh Xuân',3900000,25,N'Phòng sạch sẽ và đầy đủ tiện nghi.',N'Wifi, điều hòa, tủ lạnh','https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=900&q=85',20.999,105.800,N'Approved',GETDATE()),
(2,1,N'Phòng khép kín gần Hồ Tây',N'15 Xuân Diệu',N'Tây Hồ',5500000,30,N'Không gian thoáng mát gần Hồ Tây.',N'Ban công, điều hòa, máy giặt','https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=900&q=85',21.067,105.821,N'Approved',GETDATE()),
(2,1,N'Studio hiện đại khu Mỹ Đình',N'9 Lê Đức Thọ',N'Nam Từ Liêm',4600000,27,N'Khu dân cư an toàn, tiện đi lại.',N'Thang máy, bãi xe, điều hòa','https://images.unsplash.com/photo-1615874694520-474822394e73?auto=format&fit=crop&w=900&q=85',21.028,105.766,N'Approved',GETDATE()),
(2,1,N'Phòng cửa sổ lớn gần Bờ Hồ',N'25 Hàng Bài',N'Hoàn Kiếm',4900000,24,N'Vị trí trung tâm, phòng đầy đủ đồ.',N'Điều hòa, nóng lạnh, Wifi','https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=900&q=85',21.026,105.851,N'Approved',GETDATE()),
(2,1,N'Phòng tiện nghi ở Long Biên',N'80 Ngọc Lâm',N'Long Biên',3200000,26,N'Phòng tiện nghi, gần bến xe và siêu thị.',N'Điều hòa, bếp, chỗ để xe','https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=900&q=85',21.049,105.882,N'Approved',GETDATE()),
(2,1,N'Phòng đầy đủ nội thất tại Đống Đa',N'58 Nguyễn Chí Thanh',N'Đống Đa',5200000,32,N'Phòng rộng rãi, tiện nghi cho người đi làm.',N'Điều hòa, máy giặt, thang máy','https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=900&q=85',21.024,105.810,N'Approved',GETDATE());
INSERT INTO tblAnhPhong(fk_tindangID,sHinhanh,sMota) VALUES (1,'room1_giuong_tu.jpg',N'Góc giường ngủ và tủ quần áo'),(1,'room1_wc.jpg',N'Nhà vệ sinh khép kín sạch sẽ');
INSERT INTO tblPhongYeuThich(fk_userID,fk_tindangID) VALUES (3,1),(3,2);
INSERT INTO tblTinNhan(fk_nguoiGuiID,fk_nguoiNhanID,fk_tindangID,sNoidung,bDaxem) VALUES (3,2,1,N'Phòng còn không ạ? Chiều nay em qua xem được không?',1),(2,3,1,N'Phòng vẫn còn, em có thể đến xem lúc 5 giờ chiều.',0);
INSERT INTO tblBCViPham(fk_tindangID,fk_nguoiBaocaoID,fk_adminXulyID,sLydo,sNoidung,sTrangthaiXuly) VALUES (1,3,NULL,N'Trùng lặp tin đăng',N'Tin đăng xuất hiện nhiều lần trong ngày.',N'Pending');
INSERT INTO tblBCThongKe(sTieude,sLoaiThongke,dTungay,dDenngay,fk_adminLapID,iTongNguoidung,iTongChutro,iTongTindang,iTongTinDaduyet,iTongTinTuchoi,iTongBaocaoVipham,sTepBaocao) VALUES (N'Báo cáo tổng quan tháng 05/2026',N'Tháng','2026-05-01','2026-05-31',1,3,1,3,1,1,1,'bc_thang_5_2026.xlsx');

using RoomGoHanoi.Models;

namespace RoomGoHanoi.Data;

public static class DbSeeder
{
    public static void Seed(RoomGoDbContext db)
    {
        if (db.Users.Any())
            return;
        var admin = new AppUser
        {
            FullName = "Quản trị viên",
            Email = "admin@roomgo.vn",
            PasswordHash = "Admin@123",
            Role = UserRole.Admin,
            PhoneVerified = true,
        };
        var owner = new AppUser
        {
            FullName = "Nguyễn Minh Anh",
            Email = "chutro@roomgo.vn",
            PasswordHash = "Owner@123",
            Role = UserRole.Owner,
            Phone = "0901234567",
            PhoneVerified = true,
        };
        db.Users.AddRange(admin, owner);
        db.SaveChanges();
        string[] photos =
        [
            "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1560185007-cde436f6a4d0?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1600566753086-00f18fb6b3ea?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1615874694520-474822394e73?auto=format&fit=crop&w=900&q=85",
            "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=900&q=85",
        ];
        var rooms = new[]
        {
            (
                "Studio đầy đủ nội thất gần ĐH Bách Khoa",
                "45 Đại Cồ Việt",
                "Hai Bà Trưng",
                4200000m,
                28d,
                21.005d,
                105.843d
            ),
            (
                "Phòng trọ yên tĩnh gần Cầu Giấy",
                "120 Trần Thái Tông",
                "Cầu Giấy",
                3500000m,
                23d,
                21.034d,
                105.793d
            ),
            (
                "Căn studio có ban công thoáng",
                "58 Nguyễn Chí Thanh",
                "Đống Đa",
                5200000m,
                32d,
                21.024d,
                105.810d
            ),
            (
                "Phòng mới gần Đại học Hà Nội",
                "96 Nguyễn Trãi",
                "Thanh Xuân",
                3900000m,
                25d,
                20.999d,
                105.800d
            ),
            (
                "Phòng khép kín gần Hồ Tây",
                "15 Xuân Diệu",
                "Tây Hồ",
                5500000m,
                30d,
                21.067d,
                105.821d
            ),
            (
                "Studio hiện đại gần Mỹ Đình",
                "9 Lê Đức Thọ",
                "Nam Từ Liêm",
                4600000m,
                27d,
                21.028d,
                105.766d
            ),
            (
                "Phòng cửa sổ lớn gần Bờ Hồ",
                "25 Hàng Bài",
                "Hoàn Kiếm",
                4900000m,
                24d,
                21.026d,
                105.851d
            ),
            (
                "Phòng tiện nghi gần Long Biên",
                "80 Ngọc Lâm",
                "Long Biên",
                3200000m,
                26d,
                21.049d,
                105.882d
            ),
        };
        db.Listings.AddRange(
            rooms.Select(
                (r, i) =>
                    new Listing
                    {
                        OwnerId = owner.Id,
                        Title = r.Item1,
                        Address = r.Item2,
                        District = r.Item3,
                        Price = r.Item4,
                        Area = r.Item5,
                        Latitude = r.Item6,
                        Longitude = r.Item7,
                        CoverImageUrl = photos[i],
                        Description =
                            "Phòng sạch sẽ, đầy đủ nội thất cơ bản, an ninh tốt "
                            + "và có không gian sống thoáng đãng. Giá thuê rõ ràng, "
                            + "hỗ trợ xem phòng trực tiếp.",
                        Amenities = "Điều hòa, nóng lạnh, Wi-Fi, chỗ để xe",
                        Status = ListingStatus.Approved,
                    }
            )
        );
        db.SaveChanges();
    }
}

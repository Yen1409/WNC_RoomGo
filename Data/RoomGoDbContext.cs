using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomGoHanoi.Models;

namespace RoomGoHanoi.Data;

public class RoomGoDbContext(DbContextOptions<RoomGoDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<RoomImage> RoomImages => Set<RoomImage>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<ViolationReport> Reports => Set<ViolationReport>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Tạo custom converter cho UserRole
        var userRoleConverter = new ValueConverter<UserRole, string>(
            v => ConvertUserRoleToProvider(v),
            v => ConvertUserRoleFromProvider(v)
        );

        // Tạo custom converter cho ListingStatus
        var listingStatusConverter = new ValueConverter<ListingStatus, string>(
            v => ConvertListingStatusToProvider(v),
            v => ConvertListingStatusFromProvider(v)
        );

        // Tạo custom converter cho ReportStatus
        var reportStatusConverter = new ValueConverter<ReportStatus, string>(
            v => ConvertReportStatusToProvider(v),
            v => ConvertReportStatusFromProvider(v)
        );

        b.Entity<AppUser>(e =>
        {
            e.ToTable("tblUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("user_id");
            e.Property(x => x.FullName).HasColumnName("sHoten");
            e.Property(x => x.Email).HasColumnName("sEmail");
            e.Property(x => x.PasswordHash).HasColumnName("sMatkhau");
            e.Property(x => x.Phone).HasColumnName("sSdt");
            e.Property(x => x.Role)
                .HasColumnName("sVaitro")
                .HasConversion(userRoleConverter);
            e.Property(x => x.PhoneVerified).HasColumnName("bDaXacThuc");
            e.Property(x => x.sTrangThai).HasColumnName("sTrangthai");
            e.Property(x => x.CreatedAt).HasColumnName("dNgaydangky");
            e.Property(x => x.AvatarUrl).HasColumnName("sAvatar");
            e.HasIndex(x => x.Email).IsUnique();
        });
        
        b.Entity<Listing>(e =>
        {
            e.ToTable("tblTinDang");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("tindang_id");
            e.Property(x => x.OwnerId).HasColumnName("fk_userID");
            e.Property(x => x.Title).HasColumnName("sTieude");
            e.Property(x => x.Address).HasColumnName("sDiachi");
            e.Property(x => x.District).HasColumnName("sQuanhuyen");
            e.Property(x => x.Price).HasColumnName("fGiaThue").HasPrecision(18, 0);
            
            // fDientich là FLOAT trong SQL -> double trong C#
            e.Property(x => x.Area)
                .HasColumnName("fDientich")
                .HasColumnType("float");  // Đặt đúng kiểu float trong SQL
            
            e.Property(x => x.Description).HasColumnName("sMota");
            e.Property(x => x.Amenities).HasColumnName("sTienich");
            
            // fVido và fKinhdo là DECIMAL(10,7) trong SQL -> decimal trong C#
            e.Property(x => x.Latitude)
                .HasColumnName("fVido")
                .HasPrecision(10, 7);  // Giữ đúng precision của database
                
            e.Property(x => x.Longitude)
                .HasColumnName("fKinhdo")
                .HasPrecision(10, 7);  // Giữ đúng precision của database
                
            e.Property(x => x.Status)
                .HasColumnName("sTrangthaiTin")
                .HasConversion(listingStatusConverter);
            e.Property(x => x.RejectionReason).HasColumnName("sLydoTuchoi");
            e.Property(x => x.CreatedAt).HasColumnName("dNgaydang");
            e.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);
        });
        
        b.Entity<RoomImage>(e =>
        {
            e.ToTable("tblAnhPhong");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("anh_id");
            e.Property(x => x.ListingId).HasColumnName("fk_tindangID");
            e.Property(x => x.Url).HasColumnName("sHinhanh");
            e.Property(x => x.Caption).HasColumnName("sMota");
        });
        
        b.Entity<Favorite>(e =>
        {
            e.ToTable("tblPhongYeuThich");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("yeuthich_id");
            e.Property(x => x.UserId).HasColumnName("fk_userID");
            e.Property(x => x.ListingId).HasColumnName("fk_tindangID");
            e.Property(x => x.CreatedAt).HasColumnName("dNgayluu");
            e.HasIndex(x => new { x.UserId, x.ListingId }).IsUnique();
        });
        
        b.Entity<ChatMessage>(e =>
        {
            e.ToTable("tblTinNhan");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("tinnhan_id");
            e.Property(x => x.ListingId).HasColumnName("fk_tindangID");
            e.Property(x => x.SenderId).HasColumnName("fk_nguoiGuiID");
            e.Property(x => x.ReceiverId).HasColumnName("fk_nguoiNhanID");
            e.Property(x => x.Content).HasColumnName("sNoidung");
            e.Property(x => x.SentAt).HasColumnName("dThoigianGui");
            e.Property(x => x.IsRead).HasColumnName("bDaxem");
        });

        b.Entity<ViolationReport>(e =>
        {
            e.ToTable("tblBCViPham");

            e.HasKey(x => x.Id);

            e.Property(x => x.Id)
                .HasColumnName("baocaoVP_id");


            e.Property(x => x.ListingId)
                .HasColumnName("fk_tindangID");


            e.Property(x => x.ReporterId)
                .HasColumnName("fk_nguoiBaocaoID");


            e.Property(x => x.ProcessedById)
                .HasColumnName("fk_adminXulyID");


            // Reason -> sLydo
            e.Property(x => x.Reason)
                .HasColumnName("sLydo");


            // Detail -> sNoidung
            e.Property(x => x.Detail)
                .HasColumnName("sNoidung");


            // CreatedAt -> dNgayBaocao
            e.Property(x => x.CreatedAt)
                .HasColumnName("dNgayBaocao");


            // ProcessedAt -> dNgayXuly
            e.Property(x => x.ProcessedAt)
                .HasColumnName("dNgayXuly");


            e.Property(x => x.Status)
                .HasColumnName("sTrangthaiXuly")
                .HasConversion(reportStatusConverter);


            e.HasOne(x => x.Listing)
                .WithMany()
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);


            e.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.NoAction);


            e.HasOne(x => x.ProcessedBy)
                .WithMany()
                .HasForeignKey(x => x.ProcessedById)
                .OnDelete(DeleteBehavior.NoAction);

        });
    }

    private static string ConvertUserRoleToProvider(UserRole v)
    {
        return v switch
        {
            UserRole.Tenant => "Người thuê",
            UserRole.Owner => "Chủ trọ",
            UserRole.Admin => "Admin",
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }

    private static UserRole ConvertUserRoleFromProvider(string v)
    {
        return v switch
        {
            "Người thuê" => UserRole.Tenant,
            "Chủ trọ" => UserRole.Owner,
            "Admin" => UserRole.Admin,
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }

    private static string ConvertListingStatusToProvider(ListingStatus v)
    {
        return v switch
        {
            ListingStatus.Pending => "Chờ duyệt",
            ListingStatus.Approved => "Đã duyệt",
            ListingStatus.Rejected => "Từ chối",
            ListingStatus.Hidden => "Đã ẩn",
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }

    private static ListingStatus ConvertListingStatusFromProvider(string v)
    {
        return v switch
        {
            "Chờ duyệt" => ListingStatus.Pending,
            "Đã duyệt" => ListingStatus.Approved,
            "Từ chối" => ListingStatus.Rejected,
            "Đã ẩn" => ListingStatus.Hidden,
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }

    private static string ConvertReportStatusToProvider(ReportStatus v)
    {
        return v switch
        {
            ReportStatus.Pending => "Chưa xử lý",
            ReportStatus.Resolved => "Đã xử lý",
            ReportStatus.Rejected => "Từ chối",
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }

    private static ReportStatus ConvertReportStatusFromProvider(string v)
    {
        return v switch
        {
            "Chưa xử lý" => ReportStatus.Pending,
            "Đã xử lý" => ReportStatus.Resolved,
            "Từ chối" => ReportStatus.Rejected,
            _ => throw new NotSupportedException($"Giá trị {v} không được hỗ trợ")
        };
    }
}
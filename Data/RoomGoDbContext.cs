using Microsoft.EntityFrameworkCore;
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
        b.Entity<AppUser>(e =>
        {
            e.ToTable("tblUsers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("user_id");
            e.Property(x => x.FullName).HasColumnName("sHoten");
            e.Property(x => x.Email).HasColumnName("sEmail");
            e.Property(x => x.PasswordHash).HasColumnName("sMatkhau");
            e.Property(x => x.Phone).HasColumnName("sSdt");
            e.Property(x => x.Role).HasColumnName("sVaitro").HasConversion<string>();
            e.Property(x => x.PhoneVerified).HasColumnName("bDaXacThuc");
            e.Property(x => x.IsLocked).HasColumnName("bKhoa");
            e.Property(x => x.CreatedAt).HasColumnName("dNgaydangky");
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
            e.Property(x => x.Area).HasColumnName("fDientich");
            e.Property(x => x.Description).HasColumnName("sMota");
            e.Property(x => x.Amenities).HasColumnName("sTienich");
            e.Property(x => x.CoverImageUrl).HasColumnName("sAnhDaiDien");
            e.Property(x => x.Latitude).HasColumnName("fVido").HasPrecision(10, 7);
            e.Property(x => x.Longitude).HasColumnName("fKinhdo").HasPrecision(10, 7);
            e.Property(x => x.Status).HasColumnName("sTrangthaiTin").HasConversion<string>();
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
            e.Property(x => x.Id).HasColumnName("baocaoVP_id");
            e.Property(x => x.ListingId).HasColumnName("fk_tindangID");
            e.Property(x => x.ReporterId).HasColumnName("fk_nguoiBaocaoID");
            e.Property(x => x.ProcessedById).HasColumnName("fk_adminXulyID");
            e.Property(x => x.Reason).HasColumnName("sLydo");
            e.Property(x => x.Detail).HasColumnName("sNoidung");
            e.Property(x => x.Status).HasColumnName("sTrangthaiXuly").HasConversion<string>();
        });
    }
}

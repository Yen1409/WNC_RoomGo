using System.ComponentModel.DataAnnotations;

namespace RoomGoHanoi.Models;

public enum UserRole
{
    Tenant,
    Owner,
    Admin,
}

public enum ListingStatus
{
    Pending,
    Approved,
    Rejected,
    Hidden,
}

public enum ReportStatus
{
    Pending,
    Resolved,
    Rejected,
}

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Tenant;
    public bool PhoneVerified { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Listing
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public AppUser? Owner { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required]
    public string Address { get; set; } = "";

    [Required]
    public string District { get; set; } = "";

    [Range(1, 100000000)]
    public decimal Price { get; set; }

    [Range(1, 1000)]
    public double Area { get; set; }

    [Required]
    public string Description { get; set; } = "";
    public string Amenities { get; set; } = "";
    public string CoverImageUrl { get; set; } =
        "https://images.unsplash.com/photo-1524758631624-e2822e304c36?auto=format&fit=crop&w=900&q=85";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<RoomImage> Images { get; set; } = [];
}

public class RoomImage
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public string Url { get; set; } = "";
    public string? Caption { get; set; }
}

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ListingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }

    [Required, StringLength(2000)]
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public class ViolationReport
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public int ReporterId { get; set; }

    [Required]
    public string Reason { get; set; } = "";
    public string? Detail { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public int? ProcessedById { get; set; }
}

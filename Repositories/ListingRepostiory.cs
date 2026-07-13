using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;

namespace RoomGoHanoi.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly RoomGoDbContext _db;

    public ListingRepository(RoomGoDbContext db)
    {
        _db = db;
    }

    public async Task<Listing> GetByIdAsync(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing == null)
            throw new KeyNotFoundException($"Không tìm thấy tin đăng có id {id}.");

        return listing;
    }

    public async Task<Listing> GetByIdWithDetailsAsync(int id)
    {
        var listing = await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (listing == null)
            throw new KeyNotFoundException($"Không tìm thấy tin đăng có id {id}.");

        return listing;
    }

    public async Task<IEnumerable<Listing>> GetAllAsync()
    {
        return await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Listing>> GetByOwnerIdAsync(int ownerId)
    {
        return await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Listing>> GetByStatusAsync(ListingStatus status)
    {
        return await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Listing>> GetApprovedAsync()
    {
        return await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.Status == ListingStatus.Approved)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Listing>> GetPendingAsync()
    {
        return await _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.Status == ListingStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Listing>> GetWithImagesAsync()
    {
        return await _db.Listings
            .Include(x => x.Images)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Listing listing)
    {
        await _db.Listings.AddAsync(listing);
    }

    public async Task UpdateAsync(Listing listing)
    {
        _db.Listings.Update(listing);
    }

    public async Task DeleteAsync(Listing listing)
    {
        _db.Listings.Remove(listing);
    }

    public async Task SoftDeleteAsync(int id)
    {
        var listing = await GetByIdAsync(id);
        if (listing != null)
        {
            listing.Status = ListingStatus.Hidden;
        }
    }

    public async Task HideAsync(int id)
    {
        var listing = await GetByIdAsync(id);
        if (listing != null)
        {
            listing.Status = ListingStatus.Hidden;
        }
    }

    public async Task UnhideAsync(int id)
    {
        var listing = await GetByIdAsync(id);
        if (listing != null)
        {
            listing.Status = ListingStatus.Approved;
        }
    }

    public async Task<IEnumerable<Listing>> SearchAsync(string? keyword, string? district, 
                                                        decimal? minPrice, decimal? maxPrice)
    {
        var query = _db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.Status == ListingStatus.Approved);

        if (!string.IsNullOrWhiteSpace(district))
            query = query.Where(x => x.District == district);

        if (minPrice.HasValue)
            query = query.Where(x => x.Price >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price <= maxPrice);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordLower = keyword.ToLower();
            query = query.Where(x => 
                x.Title.ToLower().Contains(keywordLower) ||
                x.Address.ToLower().Contains(keywordLower) ||
                x.District.ToLower().Contains(keywordLower)
            );
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _db.Listings.CountAsync();
    }

    public async Task<int> CountByStatusAsync(ListingStatus status)
    {
        return await _db.Listings.CountAsync(x => x.Status == status);
    }

    public async Task<int> CountByOwnerIdAsync(int ownerId)
    {
        return await _db.Listings.CountAsync(x => x.OwnerId == ownerId);
    }

    public async Task AddImageAsync(RoomImage image)
    {
        await _db.RoomImages.AddAsync(image);
    }

    public async Task RemoveImageAsync(int imageId)
    {
        var image = await _db.RoomImages.FindAsync(imageId);
        if (image != null)
        {
            _db.RoomImages.Remove(image);
        }
    }

    public async Task<IEnumerable<RoomImage>> GetImagesByListingIdAsync(int listingId)
    {
        return await _db.RoomImages
            .Where(x => x.ListingId == listingId)
            .ToListAsync();
    }

    public async Task ModerateAsync(int id, bool approve, string? reason)
    {
        var listing = await GetByIdAsync(id);
        if (listing != null)
        {
            listing.Status = approve ? ListingStatus.Approved : ListingStatus.Rejected;
            listing.RejectionReason = approve ? null : reason;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
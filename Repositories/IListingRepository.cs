using RoomGoHanoi.Models;

namespace RoomGoHanoi.Repositories;

public interface IListingRepository
{
    // CRUD Operations
    Task<Listing> GetByIdAsync(int id);
    Task<Listing> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Listing>> GetAllAsync();
    Task<IEnumerable<Listing>> GetByOwnerIdAsync(int ownerId);
    Task<IEnumerable<Listing>> GetByStatusAsync(ListingStatus status);
    Task<IEnumerable<Listing>> GetApprovedAsync();
    Task<IEnumerable<Listing>> GetPendingAsync();
    Task<IEnumerable<Listing>> GetWithImagesAsync();
    
    // Add/Update/Delete
    Task AddAsync(Listing listing);
    Task UpdateAsync(Listing listing);
    Task DeleteAsync(Listing listing);
    Task SoftDeleteAsync(int id);
    Task HideAsync(int id);
    Task UnhideAsync(int id);
    
    // Search
    Task<IEnumerable<Listing>> SearchAsync(string? keyword, string? district, 
                                           decimal? minPrice, decimal? maxPrice);
    
    // Count
    Task<int> CountAsync();
    Task<int> CountByStatusAsync(ListingStatus status);
    Task<int> CountByOwnerIdAsync(int ownerId);
    
    // Images
    Task AddImageAsync(RoomImage image);
    Task RemoveImageAsync(int imageId);
    Task<IEnumerable<RoomImage>> GetImagesByListingIdAsync(int listingId);
    
    // Moderate
    Task ModerateAsync(int id, bool approve, string? reason);
    
    // Save
    Task SaveChangesAsync();
}
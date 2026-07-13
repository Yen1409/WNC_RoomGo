using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.Services;
using RoomGoHanoi.ViewModels;

namespace RoomGoHanoi.Controllers;

public class HomeController(RoomGoDbContext db, IGeocodingService geocoding) : Controller
{
    public async Task<IActionResult> Index() =>
        View(
            await db
                .Listings.Where(x => x.Status == ListingStatus.Approved)
                .Include(x => x.Images)
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .ToListAsync()
        );

    public IActionResult AccessDenied() => View();

    public IActionResult Error() => View();

    [HttpGet]
    public async Task<IActionResult> Search(SearchVm q)
    {
        var query = db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner)
            .Where(x => x.Status == ListingStatus.Approved);
            
        if (!string.IsNullOrWhiteSpace(q.District))
            query = query.Where(x => x.District == q.District);
        if (q.MinPrice.HasValue)
            query = query.Where(x => x.Price >= q.MinPrice);
        if (q.MaxPrice.HasValue)
            query = query.Where(x => x.Price <= q.MaxPrice);
            
        var allRooms = await query.ToListAsync();
        var result = allRooms;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.Trim();
            var directMatches = allRooms
                .Where(x =>
                    x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || x.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    || x.District.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
            if (directMatches.Count > 0)
            {
                result = directMatches;
            }
            else
            {
                var point = await geocoding.FindAsync(keyword);
                if (point is not null)
                {
                    result = allRooms
                        .Where(x => x.Latitude.HasValue && x.Longitude.HasValue)
                        .OrderBy(x =>
                            Distance(
                                point.Value.Lat,
                                point.Value.Lng,
                                (double)x.Latitude!.Value,  
                                (double)x.Longitude!.Value  
                            )
                        )
                        .Take(8)
                        .ToList();
                    ViewBag.SearchHint =
                        $"Không có tin đăng đúng tại “{keyword}”. Đây là các phòng gần vị trí bạn tìm.";
                }
                else
                {
                    ViewBag.SearchHint =
                        $"Chưa xác định được “{keyword}”. Hiển thị các phòng phù hợp với bộ lọc hiện có.";
                }
            }
        }

        if (q.Latitude.HasValue && q.Longitude.HasValue && q.RadiusKm.HasValue)
            result = result
                .Where(x =>
                    x.Latitude.HasValue
                    && x.Longitude.HasValue
                    && Distance(
                        q.Latitude.Value,
                        q.Longitude.Value,
                        (double)x.Latitude!.Value,    
                        (double)x.Longitude!.Value  
                    ) <= q.RadiusKm.Value
                )
                .ToList();
        return View((q, result));
    }

    [HttpPost, ActionName("Search")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SearchPost(SearchVm q) => await Search(q);

    public async Task<IActionResult> Detail(int id)
    {
        var listing = await db
            .Listings.Include(x => x.Owner)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == ListingStatus.Approved);
        return listing is null ? NotFound() : View(listing);
    }

    private static double Distance(double a, double b, double c, double d)
    {
        
        const double earthRadiusKm = 6371;
        var radians = Math.PI / 180;
        var h =
            Math.Sin((c - a) * radians / 2) * Math.Sin((c - a) * radians / 2)
            + Math.Cos(a * radians)
                * Math.Cos(c * radians)
                * Math.Sin((d - b) * radians / 2)
                * Math.Sin((d - b) * radians / 2);
        return 2 * earthRadiusKm * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }
}

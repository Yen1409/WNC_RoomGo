using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.Services;

namespace RoomGoHanoi.Controllers;

[Authorize(Roles = "Owner,Admin")]
public class ListingsController(RoomGoDbContext db, IGeocodingService geocoding) : Controller
{
    int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Mine() =>
        View(
            await db
                .Listings.Where(x => x.OwnerId == UserId())
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync()
        );

    public IActionResult Create() => View(new Listing());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Listing room, List<IFormFile>? images)
    {
        room.OwnerId = UserId();
        room.Status = ListingStatus.Pending;
        ModelState.Remove(nameof(room.Latitude));
        ModelState.Remove(nameof(room.Longitude));
        
        if (!ModelState.IsValid)
            return View(room);
            
        var point = await geocoding.FindAsync(room.Address + ", " + room.District);
        if (point is null)
        {
            ModelState.AddModelError(
                nameof(room.Address),
                "Không tìm thấy địa chỉ. Hãy nhập địa chỉ cụ thể hơn (số nhà, tên đường, quận)."
            );
            return View(room);
        }
        
        room.Latitude = (decimal)point.Value.Lat;   // Ép kiểu từ double sang decimal
        room.Longitude = (decimal)point.Value.Lng;
        
        db.Listings.Add(room);
        await db.SaveChangesAsync();
        
        // Xử lý upload ảnh
        if (images != null && images.Count > 0)
        {
            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    // Lưu ảnh vào wwwroot/images/rooms/
                    var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "rooms", fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    
                    var roomImage = new RoomImage
                    {
                        ListingId = room.Id,
                        Url = $"/images/rooms/{fileName}",
                        Caption = image.FileName
                    };
                    
                    db.RoomImages.Add(roomImage);
                }
            }
            await db.SaveChangesAsync();
        }
        
        TempData["Success"] = "Tin đã gửi, chờ quản trị viên duyệt.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id)
    {
        var x = await db.Listings.FindAsync(id);
        if (x is null || x.OwnerId != UserId())
            return Forbid();
        x.Status = ListingStatus.Hidden;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Mine));
    }
}
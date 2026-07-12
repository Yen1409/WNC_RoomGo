using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.Services;

namespace RoomGoHanoi.Controllers;

[Authorize]  // Yêu cầu đăng nhập
public class ListingsController(RoomGoDbContext db, IGeocodingService geocoding) : Controller
{
    int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    bool IsAdmin() => User.IsInRole("Admin");
    bool IsOwner() => User.IsInRole("Owner");

    // Owner: Xem tin của mình
    // Admin: Xem tất cả tin (để duyệt)
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Mine()
    {
        IQueryable<Listing> query = db.Listings
            .Include(x => x.Images)
            .Include(x => x.Owner);

        if (IsOwner())
        {
            query = query.Where(x => x.OwnerId == UserId());
        }

        return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    // Owner: Tạo tin mới
    [Authorize(Roles = "Owner")]
    public IActionResult Create()
    {
        // Khôi phục dữ liệu từ Session nếu có
        if (TempData["ImagesData"] != null)
        {
            ViewBag.ImagePreviews = TempData["ImagesData"] as List<string>;
        }
        return View(new Listing());
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangTin(Listing room, List<IFormFile>? images)
    {
        room.OwnerId = UserId();
        room.Status = ListingStatus.Pending;
        ModelState.Remove(nameof(room.Latitude));
        ModelState.Remove(nameof(room.Longitude));
        
        if (!ModelState.IsValid)
        {
            // Lưu ảnh đã chọn vào TempData để hiển thị lại
            if (images != null && images.Count > 0)
            {
                var imageDataList = new List<string>();
                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await image.CopyToAsync(memoryStream);
                        var base64 = Convert.ToBase64String(memoryStream.ToArray());
                        imageDataList.Add(base64);
                    }
                }
                TempData["ImagesData"] = imageDataList;
                TempData["ImageCount"] = images.Count;
            }
            return View("Create", room);
        }
            
        var point = await geocoding.FindAsync(room.Address + ", " + room.District);
        if (point is null)
        {
            ModelState.AddModelError(
                nameof(room.Address),
                "Không tìm thấy địa chỉ. Hãy nhập địa chỉ cụ thể hơn (số nhà, tên đường, quận)."
            );
            return View("Create", room);
        }
        
        room.Latitude = (decimal)point.Value.Lat;
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
        
        // Xóa dữ liệu tạm
        TempData.Remove("ImagesData");
        TempData.Remove("ImageCount");
        
        TempData["Success"] = "Tin đã được đăng và đang chờ quản trị viên duyệt.";
        return RedirectToAction(nameof(Mine));
    }

    // Các action khác giữ nguyên...
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Edit(int id)
    {
        var listing = await db.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId());
        
        if (listing is null)
            return NotFound();
            
        return View(listing);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Listing room, List<IFormFile>? images)
    {
        if (id != room.Id)
            return NotFound();

        var existingRoom = await db.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId());
            
        if (existingRoom is null)
            return NotFound();

        existingRoom.Title = room.Title;
        existingRoom.Address = room.Address;
        existingRoom.District = room.District;
        existingRoom.Price = room.Price;
        existingRoom.Area = room.Area;
        existingRoom.Description = room.Description;
        existingRoom.Amenities = room.Amenities;
        existingRoom.Status = ListingStatus.Pending;

        var point = await geocoding.FindAsync(room.Address + ", " + room.District);
        if (point is not null)
        {
            existingRoom.Latitude = (decimal)point.Value.Lat;
            existingRoom.Longitude = (decimal)point.Value.Lng;
        }

        if (images != null && images.Count > 0)
        {
            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "rooms", fileName);
                    
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    
                    var roomImage = new RoomImage
                    {
                        ListingId = existingRoom.Id,
                        Url = $"/images/rooms/{fileName}",
                        Caption = image.FileName
                    };
                    
                    db.RoomImages.Add(roomImage);
                }
            }
            await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();
        TempData["Success"] = "Tin đăng đã được cập nhật và gửi lại để duyệt.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await db.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId());
            
        if (x is null)
            return NotFound();

        foreach (var img in x.Images)
        {
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", img.Url.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        db.Listings.Remove(x);
        await db.SaveChangesAsync();
        
        TempData["Success"] = "Tin đăng đã được xóa.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id)
    {
        var x = await db.Listings.FindAsync(id);
        if (x is null || x.OwnerId != UserId())
            return Forbid();
        x.Status = ListingStatus.Hidden;
        await db.SaveChangesAsync();
        TempData["Success"] = "Tin đăng đã được ẩn.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var image = await db.RoomImages.FindAsync(id);
        if (image is null)
            return NotFound();

        var listing = await db.Listings.FindAsync(image.ListingId);
        if (listing is null || listing.OwnerId != UserId())
            return Forbid();

        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        db.RoomImages.Remove(image);
        await db.SaveChangesAsync();
        
        return Json(new { success = true });
    }
}
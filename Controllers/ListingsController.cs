using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.Services;
using RoomGoHanoi.Helpers;

namespace RoomGoHanoi.Controllers;

[Authorize]
public class ListingsController(RoomGoDbContext db, IGeocodingService geocoding) : Controller
{
    int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    bool IsAdmin() => User.IsInRole("Admin");
    bool IsOwner() => User.IsInRole("Owner");

    // Helper: Kiểm tra file ảnh an toàn
    private bool IsImageSafe(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext)) return false;

        var allowedTypes = new[] { 
            "image/jpeg", "image/png", "image/gif", 
            "image/webp", "image/svg+xml"
        };
        if (!allowedTypes.Contains(file.ContentType)) return false;

        if (file.Length > 5 * 1024 * 1024) return false; // 5MB

        return true;
    }

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

    [Authorize(Roles = "Owner")]
    public IActionResult Create()
    {
        if (TempData["ImagesData"] != null)
        {
            ViewBag.ImagePreviews = TempData["ImagesData"] as List<string>;
        }
        return View(new Listing());
    }

    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> DangTin(Listing room, List<IFormFile>? images)
    {
        // Sanitize input
        room.Title = HtmlSanitizerHelper.Sanitize(room.Title);
        room.Address = HtmlSanitizerHelper.Sanitize(room.Address);
        room.District = HtmlSanitizerHelper.Sanitize(room.District);
        room.Description = HtmlSanitizerHelper.Sanitize(room.Description);
        room.Amenities = HtmlSanitizerHelper.Sanitize(room.Amenities);

        room.OwnerId = UserId();
        room.Status = ListingStatus.Pending;
        ModelState.Remove(nameof(room.Latitude));
        ModelState.Remove(nameof(room.Longitude));

        // KIỂM TRA MODEL STATE TRƯỚC
        if (!ModelState.IsValid)
        {
            // Lưu ảnh tạm
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

        // KIỂM TRA ẢNH CÓ ĐƯỢC GỬI LÊN KHÔNG
        if (images == null || images.Count == 0)
        {
            TempData["Warning"] = "Bạn chưa chọn ảnh cho phòng. Bạn có thể thêm sau khi chỉnh sửa.";
            // Vẫn cho phép đăng tin, nhưng thông báo
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

        // XỬ LÝ UPLOAD ẢNH
        if (images != null && images.Count > 0)
        {
            foreach (var image in images)
            {
                if (image.Length > 0)
                {
                    // Kiểm tra file ảnh an toàn
                    if (!IsImageSafe(image))
                    {
                        TempData["Error"] = $"File {image.FileName} không hợp lệ. Chỉ chấp nhận ảnh JPG, PNG, GIF, WEBP dưới 5MB.";
                        continue;
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
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
                        Caption = HtmlSanitizerHelper.Sanitize(image.FileName)
                    };

                    db.RoomImages.Add(roomImage);
                }
            }
            await db.SaveChangesAsync();
            TempData["Success"] = $"Tin đã được đăng với {images.Count} ảnh và đang chờ quản trị viên duyệt.";
        }
        else
        {
            TempData["Success"] = "Tin đã được đăng và đang chờ quản trị viên duyệt. Bạn có thể thêm ảnh sau.";
        }

        TempData.Remove("ImagesData");
        TempData.Remove("ImageCount");

        return RedirectToAction(nameof(Mine));
    }
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

        // Sanitize input
        room.Title = HtmlSanitizerHelper.Sanitize(room.Title);
        room.Address = HtmlSanitizerHelper.Sanitize(room.Address);
        room.District = HtmlSanitizerHelper.Sanitize(room.District);
        room.Description = HtmlSanitizerHelper.Sanitize(room.Description);
        room.Amenities = HtmlSanitizerHelper.Sanitize(room.Amenities);

        var existingRoom = await db.Listings
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId());

        if (existingRoom is null)
            return NotFound();

        // Kiểm tra nội dung độc hại
        if (HtmlSanitizerHelper.ContainsDangerousContent(room.Description))
        {
            ModelState.AddModelError("Description", "Mô tả chứa nội dung không hợp lệ.");
            return View(room);
        }

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
                    if (!IsImageSafe(image))
                    {
                        TempData["Error"] = $"File {image.FileName} không hợp lệ.";
                        continue;
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
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
                        Caption = HtmlSanitizerHelper.Sanitize(image.FileName)
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

    // AJAX DELETE
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var x = await db.Listings
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId());

            if (x is null)
                return Json(new { success = false, message = "Không tìm thấy tin đăng." });

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

            return Json(new { success = true, message = "Tin đăng đã được xóa." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // AJAX TOGGLE HIDE/SHOW
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id)
    {
        try
        {
            var x = await db.Listings.FindAsync(id);
            if (x is null || x.OwnerId != UserId())
                return Json(new { success = false, message = "Không tìm thấy tin đăng." });

            if (x.Status == ListingStatus.Hidden)
            {
                x.Status = ListingStatus.Approved;
                await db.SaveChangesAsync();
                return Json(new { success = true, message = "Tin đăng đã được hiện lại." });
            }

            x.Status = ListingStatus.Hidden;
            await db.SaveChangesAsync();

            return Json(new { success = true, message = "Tin đăng đã được ẩn." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // AJAX Delete Image
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var image = await db.RoomImages.FindAsync(id);
            if (image is null)
                return Json(new { success = false, message = "Không tìm thấy ảnh." });

            var listing = await db.Listings.FindAsync(image.ListingId);
            if (listing is null || listing.OwnerId != UserId())
                return Json(new { success = false, message = "Bạn không có quyền xóa ảnh này." });

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", image.Url.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            db.RoomImages.Remove(image);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa ảnh thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }
}
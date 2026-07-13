using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.Services;
using RoomGoHanoi.Helpers;
using RoomGoHanoi.Repositories;

namespace RoomGoHanoi.Controllers;

[Authorize]
public class ListingsController : Controller
{
    private readonly IListingRepository _listingRepository;
    private readonly IGeocodingService _geocoding;
    private readonly IWebHostEnvironment _env;

    public ListingsController(IListingRepository listingRepository, 
                               IGeocodingService geocoding,
                               IWebHostEnvironment env)
    {
        _listingRepository = listingRepository;
        _geocoding = geocoding;
        _env = env;
    }

    int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    bool IsAdmin() => User.IsInRole("Admin");
    bool IsOwner() => User.IsInRole("Owner");

    private bool TryParsePrice(string? rawValue, out decimal price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        var raw = rawValue.Trim();
        var candidates = new[]
        {
            raw,
            raw.Replace(",", "."),
            raw.Replace(".", string.Empty).Replace(",", "."),
            raw.Replace(" ", string.Empty)
        };

        foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            if (decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
                return true;

            if (decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out price))
                return true;
        }

        return false;
    }

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

    // OWNER & ADMIN: Xem danh sách tin
    [Authorize(Roles = "Owner,Admin")]
    public async Task<IActionResult> Mine()
    {
        IEnumerable<Listing> listings;

        if (IsOwner())
        {
            listings = await _listingRepository.GetByOwnerIdAsync(UserId());
        }
        else // Admin
        {
            listings = await _listingRepository.GetAllAsync();
        }

        return View(listings);
    }

    // OWNER: Tạo tin mới (GET)
    [Authorize(Roles = "Owner")]
    public IActionResult Create()
    {
        if (TempData["ImagesData"] != null)
        {
            ViewBag.ImagePreviews = TempData["ImagesData"] as List<string>;
        }
        return View(new Listing());
    }

    // OWNER: Đăng tin (POST) - AJAX
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangTin([FromForm] Listing room, [FromForm] List<IFormFile>? images)
    {
        try
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

            if (!TryParsePrice(Request.Form["Price"], out var parsedPrice))
            {
                return Json(new
                {
                    success = false,
                    message = "Giá thuê không hợp lệ. Hãy nhập số như 2500000 hoặc 2500000.00."
                });
            }

            room.Price = parsedPrice;

            if (!ModelState.IsValid)
            {
                return Json(new { 
                    success = false, 
                    message = "Vui lòng kiểm tra lại thông tin đã nhập.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var point = await _geocoding.FindAsync(room.Address + ", " + room.District);
            if (point is null)
            {
                return Json(new { 
                    success = false, 
                    message = "Không tìm thấy địa chỉ. Hãy nhập địa chỉ cụ thể hơn." 
                });
            }

            room.Latitude = (decimal)point.Value.Lat;
            room.Longitude = (decimal)point.Value.Lng;

            await _listingRepository.AddAsync(room);
            await _listingRepository.SaveChangesAsync();

            // Xử lý upload ảnh
            int imageCount = 0;
            if (images != null && images.Count > 0)
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "images", "rooms");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        if (!IsImageSafe(image))
                        {
                            continue;
                        }

                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                        var path = Path.Combine(uploadPath, fileName);

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

                        await _listingRepository.AddImageAsync(roomImage);
                        imageCount++;
                    }
                }
                await _listingRepository.SaveChangesAsync();
            }

            var message = imageCount > 0 
                ? $"Tin đã được đăng với {imageCount} ảnh và đang chờ duyệt." 
                : "Tin đã được đăng và đang chờ duyệt. Bạn có thể thêm ảnh sau.";

            return Json(new { success = true, message = message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // OWNER: Sửa tin (GET)
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Edit(int id)
    {
        var listing = await _listingRepository.GetByIdWithDetailsAsync(id);
        if (listing == null || listing.OwnerId != UserId())
        {
            return NotFound();
        }

        return View(listing);
    }

    // OWNER: Cập nhật tin (POST) - AJAX
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromForm] int id, [FromForm] Listing room, [FromForm] List<IFormFile>? images)
    {
        try
        {
            if (id != room.Id)
                return Json(new { success = false, message = "ID không hợp lệ." });

            // Sanitize input
            room.Title = HtmlSanitizerHelper.Sanitize(room.Title);
            room.Address = HtmlSanitizerHelper.Sanitize(room.Address);
            room.District = HtmlSanitizerHelper.Sanitize(room.District);
            room.Description = HtmlSanitizerHelper.Sanitize(room.Description);
            room.Amenities = HtmlSanitizerHelper.Sanitize(room.Amenities);

            if (!TryParsePrice(Request.Form["Price"], out var parsedPrice))
            {
                ModelState.AddModelError(nameof(room.Price), "Giá thuê không hợp lệ. Hãy nhập số như 2500000 hoặc 2500000.00.");
                var invalidRoom = await _listingRepository.GetByIdWithDetailsAsync(id);
                if (invalidRoom is null || invalidRoom.OwnerId != UserId())
                    return Json(new { success = false, message = "Không tìm thấy tin đăng." });

                invalidRoom.Title = room.Title;
                invalidRoom.Address = room.Address;
                invalidRoom.District = room.District;
                invalidRoom.Description = room.Description;
                invalidRoom.Amenities = room.Amenities;
                return View(invalidRoom);
            }

            room.Price = parsedPrice;

            var existingRoom = await _listingRepository.GetByIdWithDetailsAsync(id);
            if (existingRoom is null || existingRoom.OwnerId != UserId())
                return Json(new { success = false, message = "Không tìm thấy tin đăng." });

            // Cập nhật thông tin
            existingRoom.Title = room.Title;
            existingRoom.Address = room.Address;
            existingRoom.District = room.District;
            existingRoom.Price = room.Price;
            existingRoom.Area = room.Area;
            existingRoom.Description = room.Description;
            existingRoom.Amenities = room.Amenities;
            existingRoom.Status = ListingStatus.Pending;

            // Cập nhật vị trí
            var point = await _geocoding.FindAsync(room.Address + ", " + room.District);
            if (point is not null)
            {
                existingRoom.Latitude = (decimal)point.Value.Lat;
                existingRoom.Longitude = (decimal)point.Value.Lng;
            }

            // Upload ảnh mới
            if (images != null && images.Count > 0)
            {
                var uploadPath = Path.Combine(_env.WebRootPath, "images", "rooms");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                foreach (var image in images)
                {
                    if (image.Length > 0 && IsImageSafe(image))
                    {
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                        var path = Path.Combine(uploadPath, fileName);

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

                        await _listingRepository.AddImageAsync(roomImage);
                    }
                }
            }

            await _listingRepository.UpdateAsync(existingRoom);
            await _listingRepository.SaveChangesAsync();

            TempData["Success"] = "Tin đăng đã được cập nhật và gửi lại để duyệt.";
            return RedirectToAction(nameof(Mine));
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // OWNER: Xóa tin (AJAX)
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var listing = await _listingRepository.GetByIdWithDetailsAsync(id);
            if (listing is null || listing.OwnerId != UserId())
                return Json(new { success = false, message = "Không tìm thấy tin đăng." });

            // Xóa ảnh vật lý
            foreach (var img in listing.Images)
            {
                var physicalPath = Path.Combine(_env.WebRootPath, img.Url.TrimStart('/'));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
            }

            await _listingRepository.DeleteAsync(listing);
            await _listingRepository.SaveChangesAsync();

            return Json(new { success = true, message = "Tin đăng đã được xóa." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // OWNER: Ẩn/Hiện tin (AJAX)
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id)
    {
        try
        {
            var listing = await _listingRepository.GetByIdAsync(id);
            if (listing is null || listing.OwnerId != UserId())
                return Json(new { success = false, message = "Không tìm thấy tin đăng." });

            if (listing.Status == ListingStatus.Hidden)
            {
                listing.Status = ListingStatus.Approved;
                await _listingRepository.UpdateAsync(listing);
                await _listingRepository.SaveChangesAsync();
                return Json(new { success = true, message = "Tin đăng đã được hiện lại." });
            }

            listing.Status = ListingStatus.Hidden;
            await _listingRepository.UpdateAsync(listing);
            await _listingRepository.SaveChangesAsync();

            return Json(new { success = true, message = "Tin đăng đã được ẩn." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }

    // OWNER: Xóa ảnh (AJAX)
    [Authorize(Roles = "Owner")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var image = await _listingRepository.GetImagesByListingIdAsync(id);
            var imageItem = image.FirstOrDefault();
            
            if (imageItem is null)
                return Json(new { success = false, message = "Không tìm thấy ảnh." });

            var listing = await _listingRepository.GetByIdAsync(imageItem.ListingId);
            if (listing is null || listing.OwnerId != UserId())
                return Json(new { success = false, message = "Bạn không có quyền xóa ảnh này." });

            var physicalPath = Path.Combine(_env.WebRootPath, imageItem.Url.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            await _listingRepository.RemoveImageAsync(id);
            await _listingRepository.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa ảnh thành công." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        }
    }
}
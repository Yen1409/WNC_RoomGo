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
    public async Task<IActionResult> Create(Listing room)
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
        room.Latitude = point.Value.Lat;
        room.Longitude = point.Value.Lng;
        db.Listings.Add(room);
        await db.SaveChangesAsync();
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

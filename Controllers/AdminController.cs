using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;

namespace RoomGoHanoi.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(RoomGoDbContext db) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.Users = await db.Users.CountAsync();
        ViewBag.Owners = await db.Users.CountAsync(x => x.Role == UserRole.Owner);
        ViewBag.Pending = await db.Listings.CountAsync(x => x.Status == ListingStatus.Pending);
        ViewBag.Reports = await db.Reports.CountAsync(x => x.Status == ReportStatus.Pending);
        return View();
    }

    // Admin: Xem tất cả tin đăng để duyệt (bao gồm tin chờ duyệt)
    public async Task<IActionResult> Listings()
    {
        var listings = await db.Listings
            .Include(x => x.Owner)
            .Include(x => x.Images)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
            
        return View(listings);
    }

    // Admin: Duyệt tin (Approve/Reject)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Moderate(int id, bool approve, string? reason)
    {
        var x = await db.Listings.FindAsync(id);
        if (x is null)
            return NotFound();
            
        x.Status = approve ? ListingStatus.Approved : ListingStatus.Rejected;
        x.RejectionReason = approve ? null : reason;
        await db.SaveChangesAsync();
        
        TempData["Success"] = approve ? "Tin đã được duyệt thành công." : "Tin đã bị từ chối.";
        return RedirectToAction(nameof(Listings));
    }

    // Admin: Quản lý người dùng
    public async Task<IActionResult> Users() =>
        View(await db.Users.OrderBy(x => x.FullName).ToListAsync());

    // Admin: Khóa/Mở khóa tài khoản
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var u = await db.Users.FindAsync(id);
        if (u is null)
            return NotFound();
        
        u.sTrangThai = u.sTrangThai == "Đã khóa" ? "Hoạt động" : "Đã khóa";
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Users));
    }
}
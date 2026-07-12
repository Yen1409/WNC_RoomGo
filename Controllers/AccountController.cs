using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomGoHanoi.Data;
using RoomGoHanoi.Models;
using RoomGoHanoi.ViewModels;

namespace RoomGoHanoi.Controllers;

public class AccountController(RoomGoDbContext db) : Controller
{
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        vm.FullName = vm.FullName?.Trim() ?? "";
        vm.Email = vm.Email?.Trim().ToLowerInvariant() ?? "";

        if (!ModelState.IsValid)
            return View(vm);
        if (await db.Users.AnyAsync(x => x.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại");
            return View(vm);
        }
        db.Users.Add(
            new AppUser
            {
                FullName = vm.FullName,
                Email = vm.Email,
                PasswordHash = vm.Password,
                sTrangThai = "Hoạt động"
            }
        );
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        vm.Email = vm.Email?.Trim().ToLowerInvariant() ?? "";

        if (!ModelState.IsValid)
            return View(vm);
        var u = await db.Users.SingleOrDefaultAsync(x =>
            x.Email == vm.Email && x.PasswordHash == vm.Password
        );
        if (u is null || u.sTrangThai == "Đã khóa")
        {
            ModelState.AddModelError("", "Email, mật khẩu không đúng hoặc tài khoản bị khóa.");
            return View(vm);
        }
        var c = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.FullName),
            new Claim(ClaimTypes.Role, u.Role.ToString()),
            new Claim("AppInstance", Environment.ProcessId.ToString()),
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(
                new ClaimsIdentity(c, CookieAuthenticationDefaults.AuthenticationScheme)
            ),
            new AuthenticationProperties { IsPersistent = vm.RememberMe }
        );
        
        // LƯU USER ID VÀO SESSION
        HttpContext.Session.SetInt32("UserId", u.Id);
        HttpContext.Session.SetString("UserFullName", u.FullName);
        HttpContext.Session.SetString("UserRole", u.Role.ToString());
        
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Upgrade()
    {
        var u = await db.Users.FindAsync(UserId());
        return View(u);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(string phone, string otp)
    {
        if (otp != "123456")
        {
            TempData["Error"] = "Mã OTP demo là 123456.";
            return RedirectToAction(nameof(Upgrade));
        }
        var u = await db.Users.FindAsync(UserId());
        u!.Phone = phone;
        u.PhoneVerified = true;
        u.Role = UserRole.Owner;
        await db.SaveChangesAsync();
        await HttpContext.SignOutAsync();
        TempData["Success"] = "Xác thực xong, hãy đăng nhập lại để nhận quyền Chủ trọ.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Xóa session khi đăng xuất
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
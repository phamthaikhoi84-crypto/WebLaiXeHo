using Microsoft.AspNetCore.Mvc;
using DriverService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DriverService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly DriverService.Application.Services.AuthService _authService;

    public AuthController(AppDbContext context)
    {
        _context = context;
        _authService = new DriverService.Application.Services.AuthService();
    }

    // 1. API Đăng Ký (Đã cập nhật đầy đủ Họ tên, SĐT, Avatar, Bằng lái)
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request)
    {
        var isExist = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (isExist) return BadRequest("Email này đã được sử dụng!");

        var newUser = new DriverService.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = request.Password,
            Role = request.Role,
            Name = string.IsNullOrWhiteSpace(request.Name) ? request.Email.Split('@')[0].ToUpper() : request.Name,
            Phone = request.Phone,
            Avatar = request.Avatar
        };
        _context.Users.Add(newUser);

        // Nếu là tài xế, tạo thêm hồ sơ Tài xế
        if (request.Role == "Driver")
        {
            var newDriver = new DriverService.Domain.Entities.Driver
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                LicenseType = request.LicenseType.HasValue
                    ? (DriverService.Domain.Enums.LicenseType)request.LicenseType.Value
                    : DriverService.Domain.Enums.LicenseType.B2,
                LicenseImage = request.LicenseImage // Lưu ảnh bằng lái cho Admin duyệt
            };
            _context.Drivers.Add(newDriver);
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Đăng ký thành công!" });
    }

    // 2. API Đăng Nhập
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return Unauthorized("Email không tồn tại");

        if (user.PasswordHash != request.Password)
            return Unauthorized("Mật khẩu không chính xác!");

        // ✅ CHẶN TÀI XẾ BỊ ADMIN KHÓA TÀI KHOẢN
        if (user.Role == "Driver")
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (driver != null && !driver.IsActive)
            {
                return BadRequest("Tài khoản của bạn đã bị Quản trị viên khóa! Vui lòng liên hệ CSKH.");
            }
        }

        var token = _authService.GenerateToken(user.Email, user.Role, user.Id);
        return Ok(new { Token = token, Role = user.Role });
    }

    // 3. API Lấy thông tin Hồ sơ cá nhân (Load Avatar lên góc phải màn hình)
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userIdString));
        if (user == null) return NotFound();

        return Ok(new
        {
            Name = string.IsNullOrEmpty(user.Name) ? user.Email.Split('@')[0].ToUpper() : user.Name,
            Email = user.Email,
            Phone = user.Phone ?? "---",
            Avatar = user.Avatar
        });
    }

    // 4. API Cập nhật Hồ sơ cá nhân
    [HttpPost("update-profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var user = await _context.Users.FindAsync(Guid.Parse(userIdString));
        if (user == null) return NotFound();

        user.Name = request.Name;
        user.Phone = request.Phone;
        if (!string.IsNullOrEmpty(request.Avatar)) { user.Avatar = request.Avatar; }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cập nhật hồ sơ thành công!" });
    }
}

// KHAI BÁO CÁC GÓI DỮ LIỆU ĐỂ HỨNG TỪ FRONTEND MÀ KHÔNG BỊ LỖI 400
public record LoginRequest(string Email, string Password);
public record RegisterDto(string Email, string Password, string Role, int? LicenseType, string? LicenseImage, string Name, string? Phone, string? Avatar);
public record UpdateProfileRequest(string Name, string Phone, string? Avatar);
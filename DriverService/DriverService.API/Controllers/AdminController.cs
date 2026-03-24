using DriverService.Domain.Enums;
using DriverService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DriverService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")] // Chỉ Admin mới được vào đây
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context) { _context = context; }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _context.Users.CountAsync(u => u.Role == "User");
        var totalDrivers = await _context.Users.CountAsync(u => u.Role == "Driver");
        var totalRides = await _context.Rides.CountAsync(r => r.Status == RideStatus.Completed);

        // Doanh thu hệ thống (Tính 20% phí hoa hồng của các chuyến đã hoàn thành)
        var totalRevenue = await _context.Rides
            .Where(r => r.Status == RideStatus.Completed)
            .SumAsync(r => r.Price) * 0.2m;

        return Ok(new { totalUsers, totalDrivers, totalRides, totalRevenue });
    }

    [HttpGet("pending-drivers")]
    public async Task<IActionResult> GetPendingDrivers()
    {
        // Lấy danh sách tài xế có IsApproved == false
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Where(d => !d.IsApproved)
            .Select(d => new {
                Id = d.Id,
                Email = d.User.Email,
                LicenseType = d.LicenseType.ToString(),
                CreatedAt = d.Id // Tạm mượn ID làm key
            }).ToListAsync();

        return Ok(drivers);
    }

    [HttpPost("approve-driver/{id}")]
    public async Task<IActionResult> ApproveDriver(Guid id)
    {
        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null) return NotFound("Không tìm thấy tài xế");

        driver.IsApproved = true; // MỞ KHÓA TÀI XẾ
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Đã duyệt tài xế thành công!" });
    }
    // ✅ LẤY TOÀN BỘ DANH SÁCH TÀI XẾ
    [HttpGet("all-drivers")]
    public async Task<IActionResult> GetAllDrivers()
    {
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .Select(d => new {
                Id = d.Id,
                Email = d.User.Email,
                LicenseType = d.LicenseType.ToString(),
                IsApproved = d.IsApproved,
                IsActive = d.IsActive,
                Balance = d.User.Balance
            })
            .OrderByDescending(d => d.IsApproved) // Xếp người chưa duyệt hoặc mới lên trước
            .ToListAsync();

        return Ok(drivers);
    }

    // ✅ KHÓA / MỞ KHÓA TÀI KHOẢN TÀI XẾ
    [HttpPost("toggle-driver/{id}")]
    public async Task<IActionResult> ToggleDriverStatus(Guid id)
    {
        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null) return NotFound("Không tìm thấy tài xế");

        driver.IsActive = !driver.IsActive; // Đảo ngược trạng thái
        await _context.SaveChangesAsync();

        return Ok(new { Message = driver.IsActive ? "Đã mở khóa tài khoản!" : "Đã khóa tài khoản!" });
    }

    // ✅ LẤY TOÀN BỘ LỊCH SỬ CHUYẾN ĐI CỦA HỆ THỐNG
    [HttpGet("all-rides")]
    public async Task<IActionResult> GetAllRides()
    {
        var rides = await _context.Rides
            .Include(r => r.User)
            .Include(r => r.Driver).ThenInclude(d => d.User)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                PickupLocation = r.PickupLocation,
                Destination = r.Destination,
                Price = r.Price,
                Status = r.Status.ToString(),
                PaymentMethod = r.PaymentMethod,
                CustomerEmail = r.User.Email,
                DriverEmail = r.Driver != null ? r.Driver.User.Email : "Chưa có"
            })
            .ToListAsync();

        return Ok(rides);
    }
    // ✅ API XEM CHI TIẾT HỒ SƠ TÀI XẾ
    [HttpGet("driver/{id}")]
    public async Task<IActionResult> GetDriverDetail(Guid id)
    {
        var driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
        if (driver == null) return NotFound("Không tìm thấy tài xế");

        return Ok(new
        {
            Id = driver.Id,
            Email = driver.User.Email,
            LicenseType = driver.LicenseType.ToString(),
            LicenseImage = driver.LicenseImage,
            IsApproved = driver.IsApproved
        });
    }
}
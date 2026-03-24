using DriverService.Domain.Enums;
using DriverService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DriverService.API.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DriverService.Application.Interfaces;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Driver")]
public class DriverController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<RideHub, IRideHub> _hubContext;

    public DriverController(AppDbContext context, IHubContext<RideHub, IRideHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost("accept-ride/{rideId}")]
    public async Task<IActionResult> AcceptRide(Guid rideId)
    {
        // 1. Tìm chuyến xe
        var ride = await _context.Rides.FindAsync(rideId);
        if (ride == null || ride.Status != RideStatus.Pending)
            return BadRequest("Chuyến xe không tồn tại hoặc đã có người nhận.");

        // 2. Lấy thông tin tài xế từ Token
        var userId = Guid.Parse(User.FindFirstValue("id")!);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        // 👇 3. LOGIC NGHIỆP VỤ LÁI XE HỘ: KIỂM TRA BẰNG LÁI
        if (driver.LicenseType == LicenseType.B1 && ride.TransmissionType == TransmissionType.Manual)
        {
            return BadRequest("Lỗi: Bằng B1 của bạn không được phép điều khiển xe số sàn. Vui lòng bỏ qua cuốc này!");
        }

        // 4. Cập nhật trạng thái
        ride.DriverId = driver.Id;
        ride.Status = RideStatus.Accepted;

        await _context.SaveChangesAsync();

        // 5. Bắn SignalR
        await _hubContext.Clients.All.RideStatusUpdated("Accepted");

        return Ok(new { Message = "Nhận chuyến thành công!", RideId = ride.Id });
    }

    [HttpPost("complete-ride/{rideId}")]
    public async Task<IActionResult> CompleteRide(Guid rideId)
    {
        // 1. Tìm chuyến xe
        var ride = await _context.Rides.FindAsync(rideId);
        if (ride == null || ride.Status != RideStatus.Accepted)
            return BadRequest("Chuyến xe không tồn tại hoặc chưa được nhận.");

        // 2. Cập nhật trạng thái thành Completed
        ride.Status = RideStatus.Completed;
        await _context.SaveChangesAsync();

        // 3. Bắn SignalR thông báo cho Khách hàng
        await _hubContext.Clients.All.RideStatusUpdated("Completed");

        return Ok(new { Message = "Đã hoàn thành chuyến xe!" });
    }
}
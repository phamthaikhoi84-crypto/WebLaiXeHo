using DriverService.Domain.Enums;
using DriverService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DriverService.API.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DriverService.Application.Interfaces;

namespace DriverService.API.Controllers;

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
        var ride = await _context.Rides.FindAsync(rideId);
        if (ride == null || ride.Status != RideStatus.Pending)
            return BadRequest("Chuyến xe không tồn tại hoặc đã có người nhận.");

        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var userId = Guid.Parse(userIdString);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

        // 🚨 KHIÊN BẢO VỆ 1: CHỐNG SẬP SERVER NẾU KHÔNG CÓ HỒ SƠ
        if (driver == null) return BadRequest("Tài khoản chưa có hồ sơ tài xế.");

        // ✅ CHẶN: TÀI XẾ CHƯA ĐƯỢC ADMIN DUYỆT
        if (!driver.IsApproved)
            return BadRequest("Hồ sơ của bạn đang chờ Admin phê duyệt. Vui lòng quay lại sau!");

        // ✅ ĐÃ FIX LỖI "BẮT CÁ 2 TAY": KIỂM TRA TÀI XẾ CÓ ĐANG BẬN KHÔNG
        var busyRide = await _context.Rides.FirstOrDefaultAsync(r => r.DriverId == driver.Id && r.Status == RideStatus.Accepted);
        if (busyRide != null)
        {
            return BadRequest("Bạn đang thực hiện một chuyến xe khác. Vui lòng hoàn thành trước khi nhận chuyến mới!");
        }

        // 🚨 KHIÊN BẢO VỆ 2: CHẶN BẰNG B1 LÁI XE SỐ SÀN
        if (driver.LicenseType == LicenseType.B1 && ride.TransmissionType == TransmissionType.Manual)
        {
            return BadRequest("Lỗi: Bằng B1 của bạn không được phép điều khiển xe số sàn. Vui lòng bỏ qua cuốc này!");
        }

        ride.DriverId = driver.Id;
        ride.Status = RideStatus.Accepted;

        await _context.SaveChangesAsync();
        await _hubContext.Clients.All.RideStatusUpdated(ride.Id, "Accepted");

        return Ok(new { Message = "Nhận chuyến thành công!", RideId = ride.Id });
    }

    [HttpPost("complete-ride/{rideId}")]
    public async Task<IActionResult> CompleteRide(Guid rideId)
    {
        var ride = await _context.Rides.FindAsync(rideId);
        if (ride == null || ride.Status != RideStatus.Accepted)
            return BadRequest("Chuyến xe không tồn tại hoặc chưa được nhận.");

        var customer = await _context.Users.FindAsync(ride.UserId);
        var driver = await _context.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == ride.DriverId);

        if (customer != null && driver != null)
        {
            // ✅ PHÂN LUỒNG DÒNG TIỀN THEO HÌNH THỨC THANH TOÁN
            if (ride.PaymentMethod == "Wallet")
            {
                // Trả bằng ví: Trừ tiền khách, Cộng 80% cho Tài xế
                customer.Balance -= ride.Price;
                driver.User.Balance += ride.Price * 0.8m;
            }
            else
            {
                // Trả bằng tiền mặt: Khách đưa tiền mặt trực tiếp cho Tài xế (100%)
                // Nên Hệ thống sẽ truy thu 20% phí nền tảng từ ví điện tử của Tài xế
                driver.User.Balance -= ride.Price * 0.2m;
            }
        }

        ride.Status = RideStatus.Completed;
        await _context.SaveChangesAsync();
        await _hubContext.Clients.Group(ride.Id.ToString()).RideStatusUpdated(ride.Id, "Completed");

        return Ok(new { Message = "Đã hoàn thành chuyến xe & Thanh toán thành công!" });
    }
}
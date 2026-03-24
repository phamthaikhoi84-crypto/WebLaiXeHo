using DriverService.API.Hubs;
using DriverService.Application.Interfaces;
using DriverService.Domain.Entities;
using DriverService.Domain.Enums;
using DriverService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DriverService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RideController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<RideHub, IRideHub> _hubContext;

    public RideController(AppDbContext context, IHubContext<RideHub, IRideHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost("book")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> BookRide([FromBody] RideDto request)
    {
        try
        {
            var userIdString = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var customer = await _context.Users.FindAsync(Guid.Parse(userIdString));
            var ridePrice = (decimal)request.Distance * 15000m;

            // ✅ CHỈ KIỂM TRA SỐ DƯ NẾU KHÁCH CHỌN TRẢ BẰNG VÍ LMD PAY
            if (request.PaymentMethod == "Wallet" && (customer == null || customer.Balance < ridePrice))
            {
                return BadRequest("Số dư LMD Pay không đủ! Vui lòng nạp thêm tiền hoặc chọn thanh toán Tiền mặt.");
            }

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userIdString),
                PickupLocation = request.PickupLocation,
                Destination = request.Destination,
                Distance = request.Distance,
                Price = ridePrice,
                VehicleType = request.VehicleType,
                TransmissionType = request.TransmissionType,
                PaymentMethod = request.PaymentMethod,
                Status = RideStatus.Pending
            };

            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();

            var rideData = new
            {
                id = ride.Id,
                pickupLocation = ride.PickupLocation,
                destination = ride.Destination,
                distance = ride.Distance,
                price = ride.Price,
                vehicleType = ride.VehicleType,
                transmissionType = ride.TransmissionType
            };

            await _hubContext.Clients.All.ReceiveNewRideRequest(rideData);

            return Ok(new { Message = "Đặt xe thành công!", RideId = ride.Id });
        }
        catch (Exception ex)
        {
            // ✅ ÉP BACKEND TRẢ LỖI CHI TIẾT CỦA SQL SERVER RA NGOÀI
            var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return StatusCode(500, errorMessage);
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetPendingRides()
    {
        var pendingRides = await _context.Rides.Where(r => r.Status == RideStatus.Pending)
            .OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.PickupLocation, r.Destination, r.Distance, r.Price, r.VehicleType, r.TransmissionType })
            .ToListAsync();
        return Ok(pendingRides);
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<IActionResult> GetCurrentRide()
    {
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        var driverId = driver?.Id;

        // ✅ ĐÃ SỬA: Lấy thêm dữ liệu r.User (thông tin khách hàng)
        var ride = await _context.Rides
            .Include(r => r.User)
            .Include(r => r.Driver).ThenInclude(d => d.User)
            .Where(r => (r.UserId == userId && r.Status == RideStatus.Pending) ||
                        ((r.UserId == userId || (driverId != null && r.DriverId == driverId)) && r.Status == RideStatus.Accepted))
            .OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();

        if (ride == null) return Ok(new { IsEmpty = true });

        object driverInfo = null;
        object customerInfo = null;

        if (ride.Status == RideStatus.Accepted)
        {
            // Trả thông tin Tài xế cho Khách
            if (ride.Driver != null && ride.UserId == userId)
            {
                var avgRating = await _context.Rides.Where(r => r.DriverId == ride.DriverId && r.Rating.HasValue).AverageAsync(r => (double?)r.Rating) ?? 5.0;
                driverInfo = new { Name = ride.Driver.User.Email.Split('@')[0].ToUpper(), Rating = Math.Round(avgRating, 1), Phone = "0988.123.456" };
            }
            // ✅ MỚI: Trả thông tin Khách cho Tài xế
            if (ride.User != null && driverId != null && ride.DriverId == driverId)
            {
                customerInfo = new { Name = ride.User.Email.Split('@')[0].ToUpper(), Phone = "0901.999.888" }; // Tạm fake số điện thoại
            }
        }

        return Ok(new
        {
            Id = ride.Id,
            PickupLocation = ride.PickupLocation,
            Destination = ride.Destination,
            Price = ride.Price,
            Status = ride.Status.ToString(),
            Role = ride.UserId == userId ? "User" : "Driver",
            DriverInfo = driverInfo,
            CustomerInfo = customerInfo // Gửi cả 2 cục Info
        });
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetMyHistory()
    {
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = Guid.Parse(userIdString);
        var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        var driverId = driver?.Id;

        var history = await _context.Rides
            .Where(r => r.UserId == userId || (driverId != null && r.DriverId == driverId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                r.Id,
                r.PickupLocation,
                r.Destination,
                r.Distance,
                r.Price,
                Status = r.Status.ToString(),
                r.CreatedAt,
                r.Rating
            })
            .ToListAsync();

        return Ok(history);
    }

    [HttpPost("cancel/{rideId}")]
    [Authorize]
    public async Task<IActionResult> CancelRide(Guid rideId)
    {
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        var ride = await _context.Rides.FindAsync(rideId);
        if (ride == null) return NotFound("Không tìm thấy chuyến xe.");
        if (ride.UserId != userId) return Forbid();

        if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
            return BadRequest("Không thể hủy chuyến đi đã hoàn thành hoặc đã bị hủy.");

        ride.Status = RideStatus.Cancelled;
        await _context.SaveChangesAsync();
        await _hubContext.Clients.All.RideStatusUpdated(ride.Id, "Cancelled");

        return Ok(new { Message = "Đã hủy chuyến thành công!" });
    }

    [HttpPost("rate")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> RateRide([FromBody] RateDto request)
    {
        var ride = await _context.Rides.FindAsync(request.RideId);
        if (ride == null || ride.Status != RideStatus.Completed) return BadRequest("Chuyến xe chưa hoàn thành hoặc không tồn tại.");

        ride.Rating = request.Rating;
        ride.Comment = request.Comment;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Cảm ơn bạn đã đánh giá!" });
    }
}

public record RideDto(string PickupLocation, string Destination, double Distance, VehicleType VehicleType, TransmissionType TransmissionType, string PaymentMethod);
public record RateDto(Guid RideId, int Rating, string Comment);

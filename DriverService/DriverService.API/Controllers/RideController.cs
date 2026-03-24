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
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var ride = new Ride
        {
            UserId = Guid.Parse(userIdString),
            PickupLocation = request.PickupLocation,
            Destination = request.Destination,
            Distance = request.Distance,
            Price = (decimal)request.Distance * 15000m,
            VehicleType = request.VehicleType,
            TransmissionType = request.TransmissionType,
            Status = RideStatus.Pending // Đảm bảo trạng thái ban đầu là Pending
        };

        // Dùng _context để lưu vào Database thay vì rideRepo
        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();

        // Object chứa đầy đủ thông tin gửi qua SignalR cho Frontend
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

        // Bắn cho tất cả tài xế
        await _hubContext.Clients.All.ReceiveNewRideRequest(rideData);

        return Ok(new { Message = "Đặt xe thành công!", RideId = ride.Id });
    }
    [HttpGet("history")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> GetMyHistory()
    {
        // 1. Lấy ID của Khách hàng đang đăng nhập từ Token
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();
        var userId = Guid.Parse(userIdString);

        // 2. Tìm tất cả chuyến xe của khách này, sắp xếp mới nhất lên đầu
        var history = await _context.Rides
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.PickupLocation,
                r.Destination,
                r.Distance,
                r.Price,
                Status = r.Status.ToString(), // Chuyển số Enum (0,1,2,3) thành chữ (Pending, Accepted, Completed)
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(history);
    }
    // API lấy danh sách các cuốc xe ĐANG CHỜ (Pending) từ Database SQL
    [HttpGet("pending")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetPendingRides()
    {
        // Chọc thẳng xuống SQL tìm các chuyến xe có Status = Pending (Chưa ai nhận)
        var pendingRides = await _context.Rides
            .Where(r => r.Status == RideStatus.Pending)
            .OrderByDescending(r => r.CreatedAt) // Mới nhất xếp lên trên
            .Select(r => new
            {
                r.Id,
                r.PickupLocation,
                r.Destination,
                r.Distance,
                r.Price,
                r.VehicleType,
                r.TransmissionType
            })
            .ToListAsync();

        return Ok(pendingRides);
    }

}

public record RideDto(string PickupLocation, string Destination, double Distance, VehicleType VehicleType, TransmissionType TransmissionType);
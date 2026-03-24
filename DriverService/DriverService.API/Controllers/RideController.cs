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
[Authorize(Roles = "User")]
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
}

public record RideDto(string PickupLocation, string Destination, double Distance, VehicleType VehicleType, TransmissionType TransmissionType);
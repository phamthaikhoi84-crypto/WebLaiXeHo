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

            var ride = new Ride
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userIdString),
                PickupLocation = request.PickupLocation,
                Destination = request.Destination,
                Distance = request.Distance,
                Price = (decimal)request.Distance * 15000m,
                VehicleType = request.VehicleType,
                TransmissionType = request.TransmissionType,
                Status = RideStatus.Pending,
                CreatedAt = DateTime.UtcNow
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

        var ride = await _context.Rides
            .Where(r =>
                (r.UserId == userId && r.Status == RideStatus.Pending) ||
                ((r.UserId == userId || (driverId != null && r.DriverId == driverId)) && r.Status == RideStatus.Accepted)
            )
            .OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();

        if (ride == null) return Ok(new { IsEmpty = true }); // Trả về IsEmpty cho Frontend

        return Ok(new
        {
            Id = ride.Id,
            PickupLocation = ride.PickupLocation,
            Destination = ride.Destination,
            Price = ride.Price,
            Status = ride.Status.ToString(),
            Role = ride.UserId == userId ? "User" : "Driver"
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

        var history = await _context.Rides.Where(r => r.UserId == userId || (driverId != null && r.DriverId == driverId))
            .OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.PickupLocation, r.Destination, r.Distance, r.Price, Status = r.Status.ToString(), r.CreatedAt })
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
        await _hubContext.Clients.All.RideStatusUpdated("Cancelled");

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

public record RideDto(string PickupLocation, string Destination, double Distance, VehicleType VehicleType, TransmissionType TransmissionType);
public record RateDto(Guid RideId, int Rating, string Comment);
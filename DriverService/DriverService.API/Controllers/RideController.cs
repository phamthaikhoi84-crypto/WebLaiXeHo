using DriverService.Application.Interfaces;
using DriverService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DriverService.API.Hubs;
using System.Security.Claims;

namespace DriverService.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
// 👇 SỬ DỤNG PRIMARY CONSTRUCTOR CỦA C# 12 (Không cần viết hàm constructor dài dòng nữa)
public class RideController(IRideRepository rideRepo, IHubContext<RideHub, IRideHub> hubContext) : ControllerBase
{
    [HttpPost("book")]
    public async Task<IActionResult> BookRide([FromBody] RideDto request)
    {
        // 👇 KHẮC PHỤC LỖI NULL REFERENCE BẰNG CÁCH KIỂM TRA NULL HOẶC DÙNG GÁN MẶC ĐỊNH
        var userIdString = User.FindFirstValue("id");
        if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

        var ride = new Ride
        {
            UserId = Guid.Parse(userIdString),
            PickupLocation = request.PickupLocation,
            Destination = request.Destination,
            Distance = request.Distance,
            Price = (decimal)request.Distance * 15000m
        };

        await rideRepo.AddAsync(ride);

        // Bắn thông báo realtime đến tất cả tài xế
        await hubContext.Clients.Group("Drivers").ReceiveNewRideRequest(new
        {
            ride.Id,
            ride.PickupLocation,
            ride.Destination,
            Price = ride.Price
        });

        return Ok(ride);
    }
}

public record RideDto(string PickupLocation, string Destination, double Distance);
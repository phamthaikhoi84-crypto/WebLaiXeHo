using Microsoft.AspNetCore.SignalR;
using DriverService.Application.Interfaces;

namespace DriverService.API.Hubs;

public class RideHub : Hub<IRideHub>
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Driver") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
        }
        await base.OnConnectedAsync();
    }

    // ✅ MỚI: Hàm để Khách và Tài xế "Vào chung một phòng" khi cuốc xe bắt đầu
    public async Task JoinRideGroup(string rideId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, rideId);
    }

    public async Task UpdateLocation(string rideId, double lat, double lng)
    {
        // ✅ ĐÃ SỬA: Chỉ bắn tọa độ GPS cho những người ở trong Group (Phòng) của chuyến xe này
        await Clients.Group(rideId).LocationUpdated(lat, lng);
    }

    public async Task SendChatMessage(string rideId, string senderRole, string message)
    {
        // ✅ ĐÃ SỬA: Chỉ gửi tin nhắn cho người trong cùng Group (Chống lộ tin nhắn)
        await Clients.Group(rideId).ReceiveMessage(rideId, senderRole, message);
    }
}
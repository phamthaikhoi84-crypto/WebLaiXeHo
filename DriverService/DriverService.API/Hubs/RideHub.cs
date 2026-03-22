using Microsoft.AspNetCore.SignalR;
using DriverService.Application.Interfaces;

namespace DriverService.API.Hubs;

// BẮT BUỘC PHẢI CÓ : Hub<IRideHub> Ở ĐÂY
public class RideHub : Hub<IRideHub>
{
    public override async Task OnConnectedAsync()
    {
        // Khắc phục luôn cảnh báo "Dereference null" bằng dấu ?
        if (Context.User?.IsInRole("Driver") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Drivers");
        }
        await base.OnConnectedAsync();
    }
    // Tài xế sẽ gọi hàm này liên tục mỗi 5 giây
    public async Task UpdateLocation(string rideId, double lat, double lng)
    {
        // Trong thực tế, bạn sẽ lấy ID của khách hàng từ chuyến xe (rideId)
        // và gửi thẳng vào Group hoặc ConnectionId của riêng khách hàng đó.
        // Tạm thời ở đây ta broadcast cho tất cả để dễ test.
        await Clients.All.LocationUpdated(lat, lng);
    }
}
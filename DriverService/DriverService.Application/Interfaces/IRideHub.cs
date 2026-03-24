namespace DriverService.Application.Interfaces;

public interface IRideHub
{
    Task ReceiveNewRideRequest(object rideDetails);
    Task RideAccepted(object driverDetails);
    Task RideStatusUpdated(Guid rideId, string status);

    // Thêm hàm này để báo vị trí mới cho khách hàng
    Task LocationUpdated(double lat, double lng);
    Task ReceiveMessage(string rideId, string senderRole, string message);
}
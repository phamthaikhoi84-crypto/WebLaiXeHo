using DriverService.Domain.Enums;

namespace DriverService.Domain.Entities;

public class Ride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? DriverId { get; set; }

    public string PickupLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public double Distance { get; set; } // Kilometers
    public decimal Price { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Driver? Driver { get; set; }
    public VehicleType VehicleType { get; set; }
    public TransmissionType TransmissionType { get; set; }
}
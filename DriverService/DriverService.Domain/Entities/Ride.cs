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

    public virtual User? User { get; set; }
    public virtual Driver? Driver { get; set; }
    public VehicleType VehicleType { get; set; }
    public TransmissionType TransmissionType { get; set; }
    public int? Rating { get; set; } // Điểm sao (1-5)
    public string? Comment { get; set; } // Nhận xét của khách
    public string PaymentMethod { get; set; } = "Cash";
    public double PickupLat { get; set; }
    public double PickupLng { get; set; }
}
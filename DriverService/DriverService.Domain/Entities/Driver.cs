using DriverService.Domain.Enums;

namespace DriverService.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public bool IsOnline { get; set; } = false;
    public string CurrentLocation { get; set; } = string.Empty; // Có thể lưu dạng Toạ độ (Lat, Long)
    public string? AvatarUrl { get; set; }
    public LicenseType LicenseType { get; set; }
    public User User { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    public string? LicenseImage { get; set; }
}
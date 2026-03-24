namespace DriverService.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User"; // User, Driver, Admin

    public string? Phone { get; set; }
    public string? Avatar { get; set; }

    public ICollection<Ride> Rides { get; set; } = [];
    public decimal Balance { get; set; } = 0;
}

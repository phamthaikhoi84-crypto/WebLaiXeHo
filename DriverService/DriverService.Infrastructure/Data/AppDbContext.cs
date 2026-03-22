using DriverService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DriverService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Driver> Drivers { get; set; }
    public DbSet<Ride> Rides { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Seed Admin (Đã có trong hình của bạn)
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 2. Seed Khách hàng mẫu
        var customerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = customerId,
            Name = "Khách Hàng Test",
            Email = "user@test.com",
            PasswordHash = "123", // Lưu ý: Thực tế cần hash
            Role = "User"
        });

        // 3. Seed Tài xế mẫu
        var driverUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = driverUserId,
            Name = "Tài Xế Test",
            Email = "driver@test.com",
            PasswordHash = "123",
            Role = "Driver"
        });

        modelBuilder.Entity<Driver>().HasData(new Driver
        {
            Id = Guid.NewGuid(),
            UserId = driverUserId,
            IsOnline = true,
            CurrentLocation = "10.7, 106.6"
        });
    }
}
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

        // 1. Chốt chặt liên kết giữa Ride và User (Khách hàng)
        modelBuilder.Entity<Ride>()
            .HasOne(r => r.User)    // Bắt đúng thuộc tính User trong class Ride
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Chốt chặt liên kết giữa Ride và Driver (Tài xế)
        modelBuilder.Entity<Ride>()
            .HasOne(r => r.Driver)  // Bắt đúng thuộc tính Driver trong class Ride
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
using DriverService.Application.Interfaces;
using DriverService.Domain.Entities;
using DriverService.Domain.Enums;
using DriverService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DriverService.Infrastructure.Repositories;

public class RideRepository : IRideRepository
{
    private readonly AppDbContext _context;

    public RideRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Ride> AddAsync(Ride ride)
    {
        await _context.Rides.AddAsync(ride);
        await _context.SaveChangesAsync();
        return ride;
    }

    public async Task<Ride?> GetByIdAsync(Guid id) =>
        await _context.Rides.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Ride>> GetPendingRidesAsync() =>
        await _context.Rides.Where(r => r.Status == RideStatus.Pending).ToListAsync();

    public async Task UpdateAsync(Ride ride)
    {
        _context.Rides.Update(ride);
        await _context.SaveChangesAsync();
    }
}
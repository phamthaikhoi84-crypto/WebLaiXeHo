using DriverService.Domain.Entities;

namespace DriverService.Application.Interfaces;

public interface IRideRepository
{
    Task<Ride> AddAsync(Ride ride);
    Task<Ride?> GetByIdAsync(Guid id);
    Task<IEnumerable<Ride>> GetPendingRidesAsync();
    Task UpdateAsync(Ride ride);
}
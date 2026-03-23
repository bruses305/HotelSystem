using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class ServiceRepository : Repository<Service>, IServiceRepository
{
    public ServiceRepository(HotelDbContext context) : base(context) { }

    public async Task<IEnumerable<Service>> GetActiveServicesAsync()
    {
        return await _dbSet.Where(s => s.IsActive).ToListAsync();
    }
}

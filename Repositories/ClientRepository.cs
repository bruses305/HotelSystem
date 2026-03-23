using Microsoft.EntityFrameworkCore;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class ClientRepository : Repository<Client>, IClientRepository
{
    public ClientRepository(HotelDbContext context) : base(context) { }

    public async Task<Client?> GetClientWithBookingsAsync(int id)
    {
        return await _dbSet
            .Include(c => c.Bookings)
            .ThenInclude(b => b.Room)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Where(c => (c.FullName != null && c.FullName.ToLower().Contains(term)) ||
                        (c.Phone != null && c.Phone.Contains(term)) ||
                        (c.Email != null && c.Email.ToLower().Contains(term)) ||
                        (c.Passport != null && c.Passport.Contains(term)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Client>> GetTopClientsAsync(int count)
    {
        var clients = await _dbSet.ToListAsync();
        return clients.OrderByDescending(c => c.TotalSpent).Take(count);
    }
}

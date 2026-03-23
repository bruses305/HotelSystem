using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IClientRepository : IRepository<Client>
{
    Task<Client?> GetClientWithBookingsAsync(int id);
    Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm);
    Task<IEnumerable<Client>> GetTopClientsAsync(int count);
}

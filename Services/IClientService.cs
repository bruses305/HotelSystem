using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IClientService
{
    Task<IEnumerable<Client>> GetAllClientsAsync();
    Task<Client?> GetClientByIdAsync(int id);
    Task<Client> CreateClientAsync(Client client);
    Task UpdateClientAsync(Client client);
    Task DeleteClientAsync(int id);
    Task<IEnumerable<Client>> SearchClientsAsync(string searchTerm);
    Task<IEnumerable<Client>> GetTopClientsAsync(int count);
    Task<IEnumerable<Booking>> GetClientBookingsAsync(int clientId);
}

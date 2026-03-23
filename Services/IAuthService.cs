using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IAuthService
{
    Task<Employee?> LoginAsync(string login, string password);
    Task LogoutAsync();
    static Employee? CurrentEmployee { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    event EventHandler<Employee?>? AuthChanged;
}

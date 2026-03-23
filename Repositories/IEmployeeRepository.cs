using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByLoginAsync(string login);
    Task<bool> ValidatePasswordAsync(string login, string password);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(UserRole role);
}

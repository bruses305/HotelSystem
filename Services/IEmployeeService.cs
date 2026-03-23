using HotelSystem.Models.Entities;

namespace HotelSystem.Services;

public interface IEmployeeService
{
    Task<IEnumerable<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task UpdateEmployeeAsync(Employee employee);
    Task DeleteEmployeeAsync(int id);
    Task<bool> ChangePasswordAsync(int id, string oldPassword, string newPassword);
    Task<IEnumerable<Employee>> GetActiveEmployeesAsync();
    Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(UserRole role);
}

using System.Security.Cryptography;
using System.Text;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogService _logService;

    public EmployeeService(IEmployeeRepository employeeRepository, ILogService logService)
    {
        _employeeRepository = employeeRepository;
        _logService = logService;
    }

    public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
    {
        return await _employeeRepository.GetAllAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _employeeRepository.GetByIdAsync(id);
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        employee.PasswordHash = HashPassword(employee.PasswordHash);
        var created = await _employeeRepository.AddAsync(employee);
        await _logService.LogAsync(LogLevel.Medium, 
            $"Сотрудник добавлен: {employee.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "EmployeeService");
        return created;
    }

    public async Task UpdateEmployeeAsync(Employee employee)
    {
        var existingEmployee = await _employeeRepository.GetByIdAsync(employee.Id);
        if (existingEmployee != null && employee.PasswordHash != existingEmployee.PasswordHash)
        {
            employee.PasswordHash = HashPassword(employee.PasswordHash);
        }
        
        employee.UpdatedAt = DateTime.Now;
        await _employeeRepository.UpdateAsync(employee);
        await _logService.LogAsync(LogLevel.Low, 
            $"Сотрудник обновлён: {employee.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "EmployeeService");
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee != null)
        {
            await _employeeRepository.DeleteAsync(id);
            await _logService.LogAsync(LogLevel.Critical, 
                $"Сотрудник удалён: {employee.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "EmployeeService");
        }
    }

    public async Task<bool> ChangePasswordAsync(int id, string oldPassword, string newPassword)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null) return false;

        var oldHash = HashPassword(oldPassword);
        if (employee.PasswordHash != oldHash) return false;

        employee.PasswordHash = HashPassword(newPassword);
        employee.UpdatedAt = DateTime.Now;
        await _employeeRepository.UpdateAsync(employee);
        
        await _logService.LogAsync(LogLevel.Critical, 
            $"Изменение пароля у сотрудника {employee.FullName} пользователем: {AuthService.CurrentEmployee.FullName}", "EmployeeService");
        
        return true;
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _employeeRepository.GetActiveEmployeesAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(UserRole role)
    {
        return await _employeeRepository.GetEmployeesByRoleAsync(role);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

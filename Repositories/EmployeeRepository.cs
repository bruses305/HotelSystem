using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using HotelSystem.Data;
using HotelSystem.Models.Entities;

namespace HotelSystem.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(HotelDbContext context) : base(context) { }

    public async Task<Employee?> GetByLoginAsync(string login)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Login == login);
    }

    public async Task<bool> ValidatePasswordAsync(string login, string password)
    {
        var employee = await GetByLoginAsync(login);
        if (employee == null || !employee.IsActive) return false;

        var hash = HashPassword(password);
        return employee.PasswordHash == hash;
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _dbSet.Where(e => e.IsActive).ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetEmployeesByRoleAsync(UserRole role)
    {
        return await _dbSet.Where(e => e.Role == role && e.IsActive).ToListAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

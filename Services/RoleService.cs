using HotelSystem.Data;
using HotelSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem.Services;

/// <summary>
/// Сервис для управления ролями
/// </summary>
public class RoleService
{
    private readonly HotelDbContext _context;
    
    public RoleService(HotelDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Получить все роли
    /// </summary>
    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        return await _context.Roles.OrderBy(r => r.Name).ToListAsync();
    }
    
    /// <summary>
    /// Получить роль по ID
    /// </summary>
    public async Task<Role?> GetRoleByIdAsync(int id)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
    
    /// <summary>
    /// Создать новую роль
    /// </summary>
    public async Task<Role> CreateRoleAsync(string name, string description)
    {
        var role = new Role
        {
            Name = name,
            Description = description,
            IsSystem = false
        };
        
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        
        return role;
    }
    
    /// <summary>
    /// Обновить роль
    /// </summary>
    public async Task<Role?> UpdateRoleAsync(int id, string name, string description, string? backgroundColor = null, string? textColor = null)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null || role.IsSystem)
            return null;
        
        role.Name = name;
        role.Description = description;
        if (backgroundColor != null)
            role.BackgroundColor = backgroundColor;
        if (textColor != null)
            role.TextColor = textColor;
        role.UpdatedAt = DateTime.Now;
        
        await _context.SaveChangesAsync();
        return role;
    }
    
    /// <summary>
    /// Удалить роль
    /// </summary>
    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null || role.IsSystem)
            return false;
        
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Получить сотрудников с ролью
    /// </summary>
    public async Task<IEnumerable<Employee>> GetEmployeesWithRoleAsync(int roleId)
    {
        return await _context.Employees
            .Where(e => e.RoleId == roleId && e.IsActive)
            .ToListAsync();
    }
}
using HotelSystem.Data;
using HotelSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem.Services;

/// <summary>
/// Сервис для управления правами доступа
/// </summary>
public class PermissionService
{
    private readonly HotelDbContext _context;
    
    public PermissionService(HotelDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Проверка наличия права у сотрудника
    /// </summary>
    public async Task<bool> HasPermissionAsync(Employee employee, PermissionCategory category, PermissionType type)
    {
        // Админ всегда имеет все права
        if (employee.Role == UserRole.Admin)
            return true;
        
        // Если роль не установлена или нет прав - доступ запрещён
        if (employee.Role != UserRole.Custom || employee.RoleId == null)
            return false;
        
        var hasPermission = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == employee.RoleId &&
                          rp.Permission.Category == category &&
                          rp.Permission.Type == type);
        
        return hasPermission;
    }
    
    /// <summary>
    /// Проверка права на просмотр (View)
    /// </summary>
    public Task<bool> CanViewAsync(Employee employee, PermissionCategory category)
        => HasPermissionAsync(employee, category, PermissionType.View);
    
    /// <summary>
    /// Проверка права на создание (Create)
    /// </summary>
    public Task<bool> CanCreateAsync(Employee employee, PermissionCategory category)
        => HasPermissionAsync(employee, category, PermissionType.Create);
    
    /// <summary>
    /// Проверка права на редактирование (Edit)
    /// </summary>
    public Task<bool> CanEditAsync(Employee employee, PermissionCategory category)
        => HasPermissionAsync(employee, category, PermissionType.Edit);
    
    /// <summary>
    /// Проверка права на удаление (Delete)
    /// </summary>
    public Task<bool> CanDeleteAsync(Employee employee, PermissionCategory category)
        => HasPermissionAsync(employee, category, PermissionType.Delete);
    
    /// <summary>
    /// Получить все права для роли
    /// </summary>
    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync();
    }
    
    /// <summary>
    /// Получить все права
    /// </summary>
    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions.ToListAsync();
    }
    
    /// <summary>
    /// Получить права по категории
    /// </summary>
    public async Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(PermissionCategory category)
    {
        return await _context.Permissions
            .Where(p => p.Category == category)
            .ToListAsync();
    }
    
    /// <summary>
    /// Добавить право роли
    /// </summary>
    public async Task AddPermissionToRoleAsync(int roleId, int permissionId)
    {
        var exists = await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        
        if (!exists)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Удалить право у роли
    /// </summary>
    public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        var rp = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        
        if (rp != null)
        {
            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Установить права для роли (заменяет все текущие)
    /// </summary>
    public async Task SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds)
    {
        // Удаляем все текущие права
        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _context.RolePermissions.RemoveRange(existing);
        
        // Добавляем новые
        foreach (var permissionId in permissionIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
        
        await _context.SaveChangesAsync();
    }
}
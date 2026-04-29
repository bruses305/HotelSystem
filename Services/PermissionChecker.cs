using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Services;

/// <summary>
/// Хелпер для быстрой проверки прав в UI
/// </summary>
public static class PermissionChecker
{
    private static PermissionService? _permissionService;
    
    public static void Initialize()
    {
        _permissionService = ServiceLocator.GetService<PermissionService>();
    }
    
    /// <summary>
    /// Проверить право текущего сотрудника
    /// </summary>
    public static async Task<bool> HasPermissionAsync(PermissionCategory category, PermissionType type)
    {
        if (_permissionService == null)
        {
            _permissionService = ServiceLocator.GetService<PermissionService>();
        }
        
        var employee = AuthService.CurrentEmployee;
        if (employee == null) return false;
        
        return await _permissionService.HasPermissionAsync(employee, category, type);
    }
    
    /// <summary>
    /// Проверить право на просмотр
    /// </summary>
    public static Task<bool> CanViewAsync(PermissionCategory category)
        => HasPermissionAsync(category, PermissionType.View);
    
    /// <summary>
    /// Проверить право на создание
    /// </summary>
    public static Task<bool> CanCreateAsync(PermissionCategory category)
        => HasPermissionAsync(category, PermissionType.Create);
    
    /// <summary>
    /// Проверить право на редактирование
    /// </summary>
    public static Task<bool> CanEditAsync(PermissionCategory category)
        => HasPermissionAsync(category, PermissionType.Edit);
    
    /// <summary>
    /// Проверить право на удаление
    /// </summary>
    public static Task<bool> CanDeleteAsync(PermissionCategory category)
        => HasPermissionAsync(category, PermissionType.Delete);
    
    /// <summary>
    /// Синхронная проверка (для использования в событиях UI)
    /// </summary>
    public static bool HasPermission(PermissionCategory category, PermissionType type)
    {
        return HasPermissionAsync(category, type).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Синхронная проверка просмотра
    /// </summary>
    public static bool CanView(PermissionCategory category)
        => HasPermission(category, PermissionType.View);
    
    /// <summary>
    /// Синхронная проверка создания
    /// </summary>
    public static bool CanCreate(PermissionCategory category)
        => HasPermission(category, PermissionType.Create);
    
    /// <summary>
    /// Синхронная проверка редактирования
    /// </summary>
    public static bool CanEdit(PermissionCategory category)
        => HasPermission(category, PermissionType.Edit);
    
    /// <summary>
    /// Синхронная проверка удаления
    /// </summary>
    public static bool CanDelete(PermissionCategory category)
        => HasPermission(category, PermissionType.Delete);
}
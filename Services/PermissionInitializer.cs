using HotelSystem.Data;
using HotelSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem.Services;

/// <summary>
/// Сервис для инициализации прав доступа
/// ПРАВА ОПРЕДЕЛЕНЫ В КОДЕ — добавляйте новые здесь, не трогая БД
/// </summary>
public static class PermissionInitializer
{
    /// <summary>
    /// Инициализировать права в базе данных (синхронизация с кодом)
    /// </summary>
    public static async Task InitializePermissionsAsync(HotelDbContext context)
    {
        // Определяем все права в КОДЕ — здесь удобно добавлять новые
        var allPermissions = GetAllPermissions();
        
        // Получаем существующие права из БД
        var existingPermissions = await context.Permissions.ToListAsync();
        
        // Добавляем новые права (которых ещё нет в БД)
        foreach (var permission in allPermissions)
        {
            var existing = existingPermissions.FirstOrDefault(p => 
                p.Category == permission.Category && p.Type == permission.Type);
            
            if (existing == null)
            {
                // Новое право — добавляем в БД
                context.Permissions.Add(permission);
            }
            else
            {
                // Существующее право — обновляем название/описание если изменились
                existing.Name = permission.Name;
                existing.Description = permission.Description;
                existing.Resource = permission.Resource;
            }
        }
        
        await context.SaveChangesAsync();
        
        // Создаём стандартные роли если их нет
        await InitializeDefaultRolesAsync(context);
    }
    
    /// <summary>
    /// Все права системы (определены в КОДЕ, не в БД)
    /// Чтобы добавить новое право — просто добавьте строку ниже
    /// </summary>
    private static List<Permission> GetAllPermissions()
    {
        var permissions = new List<Permission>();
        
        // Бронирования
        permissions.AddRange(CreatePermissions(PermissionCategory.Bookings, "Bookings",
            ("Просмотр бронирований", "Право на просмотр списка бронирований", PermissionType.View),
            ("Создание бронирований", "Право на создание новых бронирований", PermissionType.Create),
            ("Редактирование бронирований", "Право на редактирование бронирований", PermissionType.Edit),
            ("Удаление бронирований", "Право на удаление бронирований", PermissionType.Delete)
        ));
        
        // Номера
        permissions.AddRange(CreatePermissions(PermissionCategory.Rooms, "Rooms",
            ("Просмотр номеров", "Право на просмотр списка номеров", PermissionType.View),
            ("Создание номеров", "Право на создание новых номеров", PermissionType.Create),
            ("Редактирование номеров", "Право на редактирование номеров", PermissionType.Edit),
            ("Удаление номеров", "Право на удаление номеров", PermissionType.Delete)
        ));
        
        // Клиенты
        permissions.AddRange(CreatePermissions(PermissionCategory.Clients, "Clients",
            ("Просмотр клиентов", "Право на просмотр списка клиентов", PermissionType.View),
            ("Создание клиентов", "Право на создание новых клиентов", PermissionType.Create),
            ("Редактирование клиентов", "Право на редактирование клиентов", PermissionType.Edit),
            ("Удаление клиентов", "Право на удаление клиентов", PermissionType.Delete)
        ));
        
        // Услуги
        permissions.AddRange(CreatePermissions(PermissionCategory.Services, "Services",
            ("Просмотр услуг", "Право на просмотр списка услуг", PermissionType.View),
            ("Создание услуг", "Право на создание новых услуг", PermissionType.Create),
            ("Редактирование услуг", "Право на редактирование услуг", PermissionType.Edit),
            ("Удаление услуг", "Право на удаление услуг", PermissionType.Delete)
        ));
        
        // Оплата услуг
        permissions.AddRange(CreatePermissions(PermissionCategory.ServicesPayment, "ServicesPayment",
            ("Просмотр оплаты услуг", "Право на просмотр оплаты услуг", PermissionType.View),
            ("Создание оплаты услуг", "Право на создание оплаты услуг", PermissionType.Create),
            ("Редактирование оплаты услуг", "Право на редактирование оплаты услуг", PermissionType.Edit),
            ("Удаление оплаты услуг", "Право на удаление оплаты услуг", PermissionType.Delete)
        ));
        
        // Сотрудники
        permissions.AddRange(CreatePermissions(PermissionCategory.Employees, "Employees",
            ("Просмотр сотрудников", "Право на просмотр списка сотрудников", PermissionType.View),
            ("Создание сотрудников", "Право на создание новых сотрудников", PermissionType.Create),
            ("Редактирование сотрудников", "Право на редактирование сотрудников", PermissionType.Edit),
            ("Удаление сотрудников", "Право на удаление сотрудников", PermissionType.Delete)
        ));
        
        // Финансы
        permissions.AddRange(CreatePermissions(PermissionCategory.Finance, "Finance",
            ("Просмотр финансов", "Право на просмотр финансовой информации", PermissionType.View),
            ("Создание транзакций", "Право на создание финансовых транзакций", PermissionType.Create),
            ("Редактирование транзакций", "Право на редактирование транзакций", PermissionType.Edit),
            ("Удаление транзакций", "Право на удаление транзакций", PermissionType.Delete)
        ));
        
        // Отчёты
        permissions.AddRange(CreatePermissions(PermissionCategory.Reports, "Reports",
            ("Просмотр отчётов", "Право на просмотр отчётов", PermissionType.View),
            ("Экспорт отчётов", "Право на экспорт отчётов", PermissionType.Create),
            ("Прогнозирование", "Право на использование прогнозов", PermissionType.Create)
        ));
        
        // Логи
        permissions.AddRange(CreatePermissions(PermissionCategory.Logs, "Logs",
            ("Просмотр логов", "Право на просмотр системных логов", PermissionType.View)
        ));
        
        // Настройки
        permissions.AddRange(CreatePermissions(PermissionCategory.Settings, "Settings",
            ("Просмотр настроек", "Право на просмотр настроек системы", PermissionType.View),
            ("Редактирование настроек", "Право на редактирование настроек", PermissionType.Edit)
        ));
        
        // Управление ролями
        permissions.AddRange(CreatePermissions(PermissionCategory.RoleManagement, "RoleManagement",
            ("Просмотр ролей", "Право на просмотр ролей", PermissionType.View),
            ("Создание ролей", "Право на создание ролей", PermissionType.Create),
            ("Редактирование ролей", "Право на редактирование ролей", PermissionType.Edit),
            ("Удаление ролей", "Право на удаление ролей", PermissionType.Delete)
        ));
        
        // Дополнительные расходы
        permissions.AddRange(CreatePermissions(PermissionCategory.Expenses, "Expenses",
            ("Просмотр расходов", "Право на просмотр списка расходов", PermissionType.View),
            ("Создание расходов", "Право на создание новых расходов", PermissionType.Create),
            ("Редактирование расходов", "Право на редактирование расходов", PermissionType.Edit),
            ("Удаление расходов", "Право на удаление расходов", PermissionType.Delete),
            ("Оплата расходов", "Право на оплату расходов", PermissionType.Edit)
        ));
        
        return permissions;
    }
    
    private static IEnumerable<Permission> CreatePermissions(PermissionCategory category, string resource, params (string name, string description, PermissionType type)[] defs)
    {
        return defs.Select(d => new Permission
        {
            Name = d.name,
            Description = d.description,
            Category = category,
            Type = d.type,
            Resource = resource
        });
    }
    
    private static async Task InitializeDefaultRolesAsync(HotelDbContext context)
    {
        // Получаем все права из БД (синхронизированы с кодом)
        var allPermissions = await context.Permissions.ToListAsync();
        
        // Проверяем/создаём роль Администратора
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Администратор");
        if (adminRole == null)
        {
            adminRole = new Role
            {
                Name = "Администратор",
                Description = "Полный доступ ко всем функциям системы",
                IsSystem = true,
                BackgroundColor = "#9B59B6",
                TextColor = "#FFFFFF"
            };
            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
            
            // Администратор получает ВСЕ права
            foreach (var permission in allPermissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
            await context.SaveChangesAsync();
        }
        else
        {
            // Синхронизируем права администратора — добавляем недостающие
            var existingAdminPerms = await context.RolePermissions
                .Where(rp => rp.RoleId == adminRole.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            
            foreach (var permission in allPermissions.Where(p => !existingAdminPerms.Contains(p.Id)))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
            await context.SaveChangesAsync();
        }
        
        // Создаём администратора по умолчанию если его нет
        var adminExists = await context.Employees.AnyAsync(e => e.Login == "admin");
        if (!adminExists)
        {
            var admin = new Employee
            {
                Id = 1,
                FullName = "Администратор",
                Login = "admin",
                PasswordHash = HashPassword("admin123"),
                Role = UserRole.Admin,
                RoleId = null,
                Phone = "+7 (999) 000-00-00",
                Position = "Администратор",
                Salary = 50000,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Employees.Add(admin);
            await context.SaveChangesAsync();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

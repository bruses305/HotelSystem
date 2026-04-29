namespace HotelSystem.Models.Entities;

/// <summary>
/// Тип права доступа
/// </summary>
public enum PermissionType
{
    View,      // Просмотр
    Create,    // Создание
    Edit,      // Редактирование
    Delete     // Удаление
}

/// <summary>
/// Категория прав (для группировки)
/// </summary>
public enum PermissionCategory
{
    Bookings,        // Бронирования
    Rooms,           // Номера
    Clients,         // Клиенты
    Services,        // Услуги
    ServicesPayment, // Оплата услуг
    Employees,       // Сотрудники
    Finance,         // Финансы
    Reports,         // Отчёты
    Settings,        // Настройки
    Logs,            // Логи
    RoleManagement   // Управление ролями
}

/// <summary>
/// Право доступа
/// </summary>
public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PermissionCategory Category { get; set; }
    public PermissionType Type { get; set; }
    public string Resource { get; set; } = string.Empty; // Например: "Bookings", "Rooms"
}
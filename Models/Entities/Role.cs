namespace HotelSystem.Models.Entities;

/// <summary>
/// Роль (должность) сотрудника
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = false; // Системная роль (нельзя удалять)
    
    // Цвета для отображения роли в UI
    public string BackgroundColor { get; set; } = "#3498DB"; // Фон по умолчанию (синий)
    public string TextColor { get; set; } = "#FFFFFF";        // Текст по умолчанию (белый)
    
    // Навигационные свойства
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
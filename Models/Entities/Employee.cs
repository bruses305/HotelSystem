namespace HotelSystem.Models.Entities;

public enum UserRole
{
    Admin,       // Администратор
    Custom       // Пользовательская роль (использует RoleId)
}

public class Employee : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Custom;
    public int? RoleId { get; set; } // Ссылка на кастомную роль (null для Admin)
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Навигационные свойства
    public virtual Role? RoleEntity { get; set; }
    
    // Вычисляемое свойство для отображения названия роли
    public string RoleName
    {
        get
        {
            if (Role == UserRole.Admin)
                return "Администратор";
            if (RoleId.HasValue && RoleEntity != null)
                return RoleEntity.Name;
            return "Нет роли";
        }
    }
}

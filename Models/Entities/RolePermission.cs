namespace HotelSystem.Models.Entities;

/// <summary>
/// Связь роли и права (многие-ко-многим)
/// </summary>
public class RolePermission : BaseEntity
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    
    // Навигационные свойства
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
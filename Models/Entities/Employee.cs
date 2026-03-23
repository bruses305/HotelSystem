namespace HotelSystem.Models.Entities;

public enum UserRole
{
    Admin,       // Администратор
    Employee     // Работник
}

public class Employee : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsActive { get; set; } = true;
}
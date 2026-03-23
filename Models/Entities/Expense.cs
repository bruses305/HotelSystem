namespace HotelSystem.Models.Entities;

public class Expense : BaseEntity
{
    public int RoomId { get; set; }
    public string Category { get; set; } = string.Empty; // Вода, электричество, интернет, уборка
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;

    // Навигационные свойства
    public virtual Room? Room { get; set; }
}
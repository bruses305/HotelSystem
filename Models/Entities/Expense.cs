namespace HotelSystem.Models.Entities;

public class Expense : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Название расхода
    public string Category { get; set; } = string.Empty; // Вода, электричество, интернет, уборка, аренда, обслуживание
    public decimal Amount { get; set; }
    public DateTime LastPaymentDate { get; set; } // Последняя дата оплаты
    public string Description { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = false;
    public int? RoomId { get; set; } // Опционально - привязка к номеру

    // Навигационные свойства
    public virtual Room? Room { get; set; }
}
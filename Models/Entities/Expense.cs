namespace HotelSystem.Models.Entities;

public class Expense : BaseEntity
{
    public string Category { get; set; } = string.Empty; // Из TransactionCategory
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastPaymentDate { get; set; }
    public string Name { get; set; } = string.Empty;

    // Вычисляемое свойство: оплачено если прошло меньше 30 дней с последней оплаты
    public bool IsPaid => (DateTime.Now - LastPaymentDate).TotalDays < 30;
}
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelSystem.Models.Entities;

public class Expense : BaseEntity
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastPaymentDate { get; set; }
    public string Name { get; set; } = string.Empty;

    // Поля для разбиения суммы (будут сохраняться в БД)
    public decimal? UnitPrice { get; set; }
    public decimal? Quantity { get; set; }
    public string? UnitName { get; set; } = string.Empty;
    
    // Парсинг цены (будет сохраняться в БД как JSON)
    public string? PriceSourceJson { get; set; } = string.Empty;
    
    // Вычисляемое свойство: оплачено если прошло меньше 30 дней с последней оплаты
    public bool IsPaid => (DateTime.Now - LastPaymentDate).TotalDays < 30;
}
namespace HotelSystem.Models.Entities;

public enum TransactionType
{
    Income,    // Доход
    Expense    // Расход
}

public enum TransactionCategory
{
    Booking,           // Бронирование
    AdditionalService, // Дополнительная услуга
    Salary,            // Зарплата
    Utilities,         // Коммунальные услуги
    Purchase,          // Закупки
    Maintenance    // Обслуживание
}

public class Transaction : BaseEntity
{
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public int? BookingId { get; set; }
    public int? RoomId { get; set; }
    public int? EmployeeId { get; set; }
    public int? ServiceId { get; set; }
    public int Quantity { get; set; } = 1;

    // Навигационные свойства
    public virtual Booking? Booking { get; set; }
    public virtual Room? Room { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual Service? Service { get; set; }
}
namespace HotelSystem.Models.Entities;

public enum BookingStatus
{
 Pending, // Ожидание
 Confirmed, // Подтверждено
 Paid, // Оплачено
 CheckedIn, // Заселён
 Cancelled, // Отменено
 Completed // Завершено
}

public class Booking : BaseEntity
{
    public int RoomId { get; set; }
    public int ClientId { get; set; }
    public int? CreatedById { get; set; }

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal PaidAmount { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Вычисляемые свойства
    public int Days => Math.Max(1, (CheckOutDate - CheckInDate).Days);
    public decimal RemainingAmount => TotalPrice - PaidAmount;
    public bool IsFullyPaid => PaidAmount >= TotalPrice;

    // Навигационные свойства
    public virtual Room? Room { get; set; }
    public virtual Client? Client { get; set; }
    public virtual Employee? CreatedBy { get; set; }
    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
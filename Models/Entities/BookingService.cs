namespace HotelSystem.Models.Entities;

public class BookingService : BaseEntity
{
    public int BookingId { get; set; }
    public int ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal TotalPrice { get; set; }

    // Навигационные свойства
    public virtual Booking? Booking { get; set; }
    public virtual Service? Service { get; set; }
}
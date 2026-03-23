namespace HotelSystem.Models.Entities;

public class Client : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Passport { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }

    // Навигационные свойства
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
namespace HotelSystem.Models.Entities;

public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // Навигационные свойства
    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
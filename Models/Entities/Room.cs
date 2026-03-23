namespace HotelSystem.Models.Entities;

public enum RoomType
{
    Standard,    // Стандарт
    Lux,         // Люкс
    Apartments   // Апартаменты
}

public enum RoomStatus
{
    Free,        // Свободен
    Occupied,    // Занят
    Cleaning,    // На уборке
    Repair       // В ремонте
}

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public decimal Price { get; set; }
    public RoomStatus Status { get; set; }
    public int Capacity { get; set; }
    public string Description { get; set; } = string.Empty;

    // Расходы на содержание
    public decimal WaterExpense { get; set; }
    public decimal ElectricityExpense { get; set; }
    public decimal InternetExpense { get; set; }
    public decimal CleaningExpense { get; set; }

    public decimal TotalExpenses => WaterExpense + ElectricityExpense + InternetExpense + CleaningExpense;

    // Навигационные свойства
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
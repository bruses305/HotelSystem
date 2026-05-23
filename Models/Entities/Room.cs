namespace HotelSystem.Models.Entities;

public enum RoomType
{
    Стандартный,    // Стандарт
    Люкс,         // Люкс
    Апартаменты   // Апартаменты
}

public enum RoomStatus
{
    Свободен,        // Свободен
    Занят,    // Занят
    Уборка,    // На уборке
    Ремонт       // В ремонте
}

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public decimal Profit { get; set; } = 0; // Прибыль за сутки
    public decimal Cost { get; set; } = 0; // Себестоимость за сутки (авторасчёт)
    public RoomStatus Status { get; set; }
    public int Capacity { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Area { get; set; } = 0; // Площадь помещения в м²

    // Цена = Себестоимость + Прибыль
    public decimal Price => Cost + Profit;

    // Навигационные свойства
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
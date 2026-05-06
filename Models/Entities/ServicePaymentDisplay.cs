namespace HotelSystem.Models.Entities;

/// <summary>
/// Отображение оплаты услуг для UI
/// </summary>
public class ServicePaymentDisplay
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string ClientName { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

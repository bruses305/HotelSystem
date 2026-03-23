using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ReportsView : Page
{
 private readonly IFinanceService _financeService;
 private readonly IRoomService _roomService;
 private readonly IClientService _clientService;
 private readonly IBookingService _bookingService;

 public ReportsView()
 {
 InitializeComponent();
 _financeService = ServiceLocator.GetService<IFinanceService>();
 _roomService = ServiceLocator.GetService<IRoomService>();
 _clientService = ServiceLocator.GetService<IClientService>();
 _bookingService = ServiceLocator.GetService<IBookingService>();
        
 StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-6);
 EndDatePicker.SelectedDate = DateTime.Today;
        
 LoadReportAsync();
 }

 private async void LoadReportAsync()
 {
 try
 {
 var startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
 var endDate = EndDatePicker.SelectedDate ?? DateTime.Today;

 // Загрузка данных отчёта
 var report = await _financeService.GetFinanceReportAsync(startDate, endDate);
            
 // График занятости номеров
 var occupancyModel = new PlotModel { Title = "Занятость номеров" };
 var occupancySeries = new PieSeries { StrokeThickness =2 };
 var totalRooms = (await _roomService.GetAllRoomsAsync()).Count();
 var bookings = await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate);
 var bookedDays = bookings.SelectMany(b => Enumerable.Range(0, (b.CheckOutDate - b.CheckInDate).Days)
 .Select(d => b.CheckInDate.AddDays(d))).Distinct().Count();
 var totalDays = (endDate - startDate).Days * totalRooms;
            
 occupancySeries.Slices.Add(new PieSlice("Занято", bookedDays));
 occupancySeries.Slices.Add(new PieSlice("Свободно", Math.Max(0, totalDays - bookedDays)));
 occupancyModel.Series.Add(occupancySeries);
 OccupancyChart.Model = occupancyModel;

 // График доходов по месяцам
 var incomeModel = new PlotModel { Title = "Доходы" };
 var incomeSeries = new BarSeries();
 foreach (var month in report.IncomeByMonth.OrderBy(m => m.Key))
 {
 incomeSeries.Items.Add(new BarItem { Value = (double)month.Value });
 }
 incomeModel.Series.Add(incomeSeries);
 IncomeChart.Model = incomeModel;

 // Прибыль по номерам
 var rooms = await _roomService.GetAllRoomsAsync();
 var roomProfits = rooms.Select(r => new 
 {
 Name = r.Name,
 Income = report.IncomeByRoom.ContainsKey(r.Id) ? report.IncomeByRoom[r.Id] :0,
 Expenses = r.TotalExpenses *6,
 Profit = (report.IncomeByRoom.ContainsKey(r.Id) ? report.IncomeByRoom[r.Id] :0) - (double)(r.TotalExpenses *6)
 }).ToList();
 RoomProfitGrid.ItemsSource = roomProfits;

 // Топ клиентов
 var clients = await _clientService.GetTopClientsAsync(10);
 TopClientsGrid.ItemsSource = clients;
 }
 catch (Exception ex)
 {
 MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void GenerateReport_Click(object sender, RoutedEventArgs e)
 {
 LoadReportAsync();
 }
}

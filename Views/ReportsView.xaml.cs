using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Helpers.Reports;
using HotelSystem.Repositories;
using Microsoft.Win32;
using System.IO;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class ReportsView : Page
{
    private readonly IFinanceService _financeService;
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    private readonly ILogService _logService;
    private readonly FinanceCalculator _financeCalculator;
    private ExcelExporter? _excelExporter;
    
    private DateTime _startDate;
    private DateTime _endDate;
    private FinanceReport? _currentReport;
    private string _lastExportPath = "";
    private bool _showIncomeByDay;
    private bool _showExpenses;
    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HotelSystem",
        "last_export.txt");

    public ReportsView()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _clientService = ServiceLocator.GetService<IClientService>();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        _logService = ServiceLocator.GetService<ILogService>();
        _financeCalculator = new FinanceCalculator(
            ServiceLocator.GetService<IBookingRepository>(),
            ServiceLocator.GetService<ITransactionRepository>()
        );
        
        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
        EndDatePicker.SelectedDate = DateTime.Today;
        
        _startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
        _endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
        
        LoadLastExportPath();
        LoadReportAsync();
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        // Экспорт требует права Create (т.к. создаёт файл)
        if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
        {
            if (FindName("ExportExcelBtn") is Button excelBtn)
                excelBtn.Visibility = Visibility.Collapsed;
            if (FindName("ExportChartsBtn") is Button chartsBtn)
                chartsBtn.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadLastExportPath()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                _lastExportPath = File.ReadAllText(_configPath).Trim();
                if (!string.IsNullOrEmpty(_lastExportPath) && File.Exists(_lastExportPath))
                {
                    OpenLastExportBtn.IsEnabled = true;
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    private void SaveLastExportPath(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            
            File.WriteAllText(_configPath, path);
            _lastExportPath = path;
            OpenLastExportBtn.IsEnabled = true;
        }
        catch
        {
            // ignored
        }
    }

    private void OpenLastExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_lastExportPath) && File.Exists(_lastExportPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _lastExportPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenLastExportBtn.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка открытия: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadReportAsync()
    {
        try
        {
            _startDate = StartDatePicker.SelectedDate?.Date ?? DateTime.Today.AddMonths(-1).Date;
            _endDate = EndDatePicker.SelectedDate?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.Today.Date.AddDays(1).AddSeconds(-1);

            // Финансовый отчёт
            _currentReport = await _financeService.GetFinanceReportAsync(_startDate, _endDate);
            
            // Обновляем карточки
            TotalIncomeText.Text = $"{_currentReport.TotalIncome:N0} Br";
            TotalExpensesText.Text = $"{_currentReport.TotalExpenses:N0} Br";
            ProfitText.Text = $"{_currentReport.Profit:N0} Br";
            
            // Бронирования
            var bookings = await _bookingService.GetBookingsByDateRangeAsync(_startDate, _endDate);
            var bookingsList = bookings.ToList();
            BookingsCountText.Text = bookingsList.Count.ToString();
            
            // Транзакции
            var transactions = await _financeService.GetTransactionsAsync(_startDate, _endDate);
            var transactionsList = transactions.ToList();
            
            // Загрузка отеля
            var totalRooms = (await _roomService.GetAllRoomsAsync()).Count();
            var bookedDays = bookingsList
                .SelectMany(b => Enumerable.Range(0, (b.CheckOutDate - b.CheckInDate).Days)
                .Select(d => b.CheckInDate.AddDays(d)))
                .Distinct()
                .Count();
            var totalDays = (_endDate - _startDate).Days * totalRooms;
            var occupancy = totalDays > 0 ? (double)bookedDays / totalDays * 100 : 0;
            OccupancyText.Text = $"{occupancy:F1}%";

            // График загрузки
            var occupancyModel = new PlotModel { Title = $"Загрузка: {occupancy:F1}%" };
            var occupancySeries = new PieSeries { StrokeThickness = 2 };
            occupancySeries.Slices.Add(new PieSlice("Занято", bookedDays) { Fill = OxyColor.FromRgb(52, 152, 219) });
            occupancySeries.Slices.Add(new PieSlice("Свободно", Math.Max(0, totalDays - bookedDays)) { Fill = OxyColor.FromRgb(149, 165, 166) });
            occupancyModel.Series.Add(occupancySeries);
            OccupancyChart.Model = occupancyModel;

            // График доходов
            UpdateIncomeChart();

            // Прибыльность номеров
            var rooms = await _roomService.GetAllRoomsAsync();
            var roomProfits = rooms.Select(room => new {
                Name = room.Name,
                Type = room.Type.ToString(),
                Income = (double)bookingsList.Where(b => b.RoomId == room.Id).Sum(b => b.PaidAmount) +
                         (double)transactionsList.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Income).Sum(t => t.Amount),
                Expenses = (double)transactionsList.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Expense).Sum(t => t.Amount),
                Profit = 0.0
            }).Select(r => new {
                r.Name, r.Type,
                r.Income, r.Expenses,
                Profit = r.Income - r.Expenses
            }).ToList();
            
            RoomProfitGrid.ItemsSource = roomProfits;

            // Топ клиентов
            var clientSpending = new Dictionary<int, (decimal spentOnRooms, decimal spentOnServices, int bookingsCount)>();
            
            foreach (var booking in bookingsList)
            {
                if (!clientSpending.ContainsKey(booking.ClientId))
                    clientSpending[booking.ClientId] = (0, 0, 0);
                
                var current = clientSpending[booking.ClientId];
                clientSpending[booking.ClientId] = (
                    current.spentOnRooms + booking.PaidAmount,
                    current.spentOnServices,
                    current.bookingsCount + 1
                );
            }
            
            foreach (var tx in transactionsList.Where(t => t.BookingId.HasValue))
            {
                var booking = bookingsList.FirstOrDefault(b => b.Id == tx.BookingId);
                if (booking != null && clientSpending.ContainsKey(booking.ClientId))
                {
                    var current = clientSpending[booking.ClientId];
                    clientSpending[booking.ClientId] = (
                        current.spentOnRooms,
                        current.spentOnServices + tx.Amount,
                        current.bookingsCount
                    );
                }
            }
            
            var allClients = await _clientService.GetAllClientsAsync();
            var topClients = allClients
                .Where(c => clientSpending.ContainsKey(c.Id))
                .Select(c => new {
                    FullName = c.FullName,
                    TotalSpent = clientSpending[c.Id].spentOnRooms + clientSpending[c.Id].spentOnServices,
                    BookingsCount = clientSpending[c.Id].bookingsCount
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();
            TopClientsGrid.ItemsSource = topClients;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        if (_startDate > _endDate)
        {
            MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        LoadReportAsync();
    }

    private void SwitchChart_Click(object sender, RoutedEventArgs e)
    {
        _showIncomeByDay = !_showIncomeByDay;
        
        SwitchChartBtn.Content = _showIncomeByDay ? "По месяцам" : "По дням";
        
        IncomeChartTitle.Text = _showExpenses ? 
            (_showIncomeByDay ? "Расходы по месяцам" : "Расходы по дням") : 
            (_showIncomeByDay ? "Доходы по месяцам" : "Доходы по дням");
        
        UpdateIncomeChart();
    }

    private async void ToggleExpenses_Click(object sender, RoutedEventArgs e)
    {
        _showExpenses = !_showExpenses;
        
        var btn = sender as Button;
        btn.Content = _showExpenses ? "Показать доходы" : "Показать расходы";
        
        IncomeChartTitle.Text = _showExpenses ? 
            (_showIncomeByDay ? "Расходы по месяцам" : "Расходы по дням") : 
            (_showIncomeByDay ? "Доходы по месяцам" : "Доходы по дням");
        
        UpdateIncomeChart();
    }

    private async void UpdateIncomeChart()
    {
        try
        {
            var startDate = _startDate.Date;
            var endDate = _endDate.Date.AddDays(1).AddSeconds(-1);
            
            var incomeModel = new PlotModel { Title = IncomeChartTitle.Text };
            var incomeSeries = new BarSeries { FillColor = _showExpenses ? OxyColor.FromRgb(231, 76, 60) : OxyColor.FromRgb(39, 174, 96) };
            
            if (_showIncomeByDay)
            {
                Dictionary<DateTime, decimal> periodData;
                
                if (_showExpenses)
                {
                    periodData = await _financeCalculator.GetExpenseByDayAsync(startDate, endDate);
                }
                else
                {
                    periodData = await _financeCalculator.GetIncomeByDayAsync(startDate, endDate);
                }
                
                var allDays = periodData.Keys
                    .Where(d => d >= startDate && d <= endDate)
                    .OrderBy(d => d).Take(31);
                
                foreach (var day in allDays)
                {
                    incomeSeries.Items.Add(new BarItem { Value = (double)periodData[day] });
                }
                
                // Y axis - категории (даты)
                var categoryAxis = new OxyPlot.Axes.CategoryAxis 
                {
                    Position = OxyPlot.Axes.AxisPosition.Left
                };
                foreach (var day in allDays)
                    categoryAxis.Labels.Add(day.ToString("dd.MM"));
                incomeModel.Axes.Add(categoryAxis);
                
                // X axis - числовые значения
                var linearAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "Сумма (Br)",
                    StringFormat = "0"
                };
                incomeModel.Axes.Add(linearAxis);
            }
            else
            {
                Dictionary<string, decimal> periodData;
                
                if (_showExpenses)
                {
                    periodData = await _financeCalculator.GetExpenseByMonthAsync(startDate, endDate);
                }
                else
                {
                    periodData = await _financeCalculator.GetIncomeByMonthAsync(startDate, endDate);
                }
                
                foreach (var month in periodData.OrderBy(m => m.Key))
                    incomeSeries.Items.Add(new BarItem { Value = (double)month.Value });
                
                // Y axis - категории (месяцы)
                var categoryAxis = new OxyPlot.Axes.CategoryAxis 
                {
                    Position = OxyPlot.Axes.AxisPosition.Left
                };
                string[] monthNames =
                [
                    "Янв", "Фев", "Мар", "Апр", "Май", "Июн", 
                    "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек"
                ];
                foreach (var month in periodData.OrderBy(m => m.Key))
                {
                    var monthKey = month.Key.Split('-')[1]; // Получаем MM из yyyy-MM
                    var monthIndex = int.Parse(monthKey) - 1;
                    categoryAxis.Labels.Add(monthNames[monthIndex]);
                }
                incomeModel.Axes.Add(categoryAxis);
                
                // X axis - числовые значения
                var linearAxis = new OxyPlot.Axes.LinearAxis
                {
                    Position = OxyPlot.Axes.AxisPosition.Bottom,
                    Title = "Сумма (Br)",
                    StringFormat = "0"
                };
                incomeModel.Axes.Add(linearAxis);
            }
                
            incomeModel.Series.Add(incomeSeries);
            IncomeChart.Model = incomeModel;
        }
        catch
        {
            // ignored
        }
    }

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
            {
                MessageBox.Show("Недостаточно прав для экспорта отчётов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"HotelReport_{DateTime.Now:yyyyMMdd}"
                };

                if (dialog.ShowDialog() == true)
                {
                    // Инициализируем экспортер
                    _excelExporter = new ExcelExporter(
                        _financeService, _roomService, _clientService, _bookingService, _serviceService);
                
                    await _excelExporter.ExportAsync(_startDate.Date, _endDate.Date, dialog.FileName);
                
                    SaveLastExportPath(dialog.FileName);
                
                    await _logService.LogAsync(LogLevel.Medium, 
                        $"Создан финансовый отчёт Excel: {dialog.FileName}", "ReportsView");
                
                    MessageBox.Show($"Отчёт сохранён: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch 
        {
            // ignored
        }
    }

    private async void ExportCharts_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
        {
            MessageBox.Show("Недостаточно прав для экспорта графиков!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                DefaultExt = "png",
                FileName = "HotelCharts"
            };

            if (dialog.ShowDialog() == true)
            {
                PlotModel? occupancyModel = null;
                PlotModel? incomeModel = null;
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    occupancyModel = OccupancyChart.Model;
                    incomeModel = IncomeChart.Model;
                });
                
                var basePath = Path.GetDirectoryName(dialog.FileName) ?? "";
                var baseName = Path.GetFileNameWithoutExtension(dialog.FileName);
                var savedPaths = new List<string>();
                
                if (occupancyModel != null)
                {
                    var occPath = Path.Combine(basePath, $"{baseName}_Occupancy.png");
                    await ExcelStyles.ExportToPngAsync(occupancyModel, occPath);
                    savedPaths.Add(occPath);
                }
                
                if (incomeModel != null)
                {
                    var incPath = Path.Combine(basePath, $"{baseName}_Income.png");
                    await ExcelStyles.ExportToPngAsync(incomeModel, incPath);
                    savedPaths.Add(incPath);
                }
                
                if (savedPaths.Count > 0)
                    SaveLastExportPath(savedPaths[0]);
                
                await _logService.LogAsync(LogLevel.Medium, 
                    $"Сохранены графики: {string.Join(", ", savedPaths)}", "ReportsView");
                
                MessageBox.Show("Графики сохранены!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

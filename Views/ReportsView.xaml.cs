using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Helpers;
using HotelSystem.Helpers.Reports;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace HotelSystem.Views;

public partial class ReportsView : Page
{
    #region Поля и конструктор

    private readonly IFinanceService _financeService;
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    private readonly IExpenseService _expenseService;
    private readonly ILogService _logService;
    private ExcelExporter? _excelExporter;
    
    private DateTime _startDate;
    private DateTime _endDate;
    private FinanceReport? _currentReport;
    private string _lastExportPath = "";
    private bool _showIncomeByDay;
    private bool _showExpenses;
    private bool _showOccupancyByDay;
    private bool _compareWithPrevious;
    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HotelSystem", "settings.json");
    
    private List<Booking> _currentBookingsList = new();
    private List<Transaction> _currentTransactionsList = new();
    private int occupiedRoomDays;
    private int maxPossibleRoomDays;
    
    // Для вкладки "Клиенты"
    private List<ClientData> _allClientsData = new();

    public ReportsView()
    {
        InitializeComponent();
        _financeService = ServiceLocator.GetService<IFinanceService>();
        _roomService = ServiceLocator.GetService<IRoomService>();
        _clientService = ServiceLocator.GetService<IClientService>();
        _bookingService = ServiceLocator.GetService<IBookingService>();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        _expenseService = ServiceLocator.GetService<IExpenseService>();
        _logService = ServiceLocator.GetService<ILogService>();
        
        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
        EndDatePicker.SelectedDate = DateTime.Today;
        
        _startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
        _endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
        
        LoadLastExportPath();
        LoadReportAsync();
        CheckPermissions();

        CompareWithPreviousCheckBox.Checked += (s, e) => _compareWithPrevious = true;
        CompareWithPreviousCheckBox.Unchecked += (s, e) => _compareWithPrevious = false;
    }

    #endregion

    #region Вспомогательные методы (разрешения, пути, настройки)

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
        {
            if (FindName("ExportExcelBtn") is Button excelBtn) excelBtn.Visibility = Visibility.Collapsed;
            if (FindName("ExportChartsBtn") is Button chartsBtn) chartsBtn.Visibility = Visibility.Collapsed;
            if (FindName("ExportPdfBtn") is Button pdfBtn) pdfBtn.Visibility = Visibility.Collapsed;
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
                    OpenLastExportBtn.IsEnabled = true;
            }
        }
        catch { }
    }

    private void SaveLastExportPath(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            File.WriteAllText(_configPath, path);
            _lastExportPath = path;
            OpenLastExportBtn.IsEnabled = true;
        }
        catch { }
    }

    private void OpenLastExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_lastExportPath) && File.Exists(_lastExportPath))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _lastExportPath, UseShellExecute = true });
            else
            {
                MessageBox.Show("Файл не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenLastExportBtn.IsEnabled = false;
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка открытия: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private string GetChangeText(decimal current, decimal previous)
    {
        if (previous == 0) return "▲ 100%";
        var change = (double)((current - previous) / previous * 100);
        var arrow = change >= 0 ? "▲" : "▼";
        return $"{arrow} {Math.Abs(change):F1}%";
    }

    #endregion

    #region Основная загрузка данных

    private async void LoadReportAsync()
    {
        try
        {
            _startDate = StartDatePicker.SelectedDate?.Date ?? DateTime.Today.AddMonths(-1).Date;
            _endDate = EndDatePicker.SelectedDate?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.Today.Date.AddDays(1).AddSeconds(-1);

            _currentReport = await _financeService.GetFinanceReportAsync(_startDate, _endDate);
            
            TotalIncomeText.Text = AppConstants.FormatPrice(_currentReport.TotalIncome);
            TotalExpensesText.Text = AppConstants.FormatPrice(_currentReport.TotalExpenses);
            ProfitText.Text = AppConstants.FormatPrice(_currentReport.Profit);
            
            if (_compareWithPrevious)
            {
                var prevStart = _startDate.AddDays(-(_endDate - _startDate).Days - 1);
                var prevEnd = _startDate.AddDays(-1);
                var prevBookings = await _bookingService.GetBookingsByDateRangeAsync(prevStart, prevEnd);
                var prevTransactions = await _financeService.GetTransactionsAsync(prevStart, prevEnd);
                decimal prevIncome = prevBookings.Sum(b => b.PaidAmount) +
                                     prevTransactions.Where(t => t.Type == TransactionType.Доход && t.Category == TransactionCategory.Дополнительная_услуга).Sum(t => t.Amount);
                decimal prevExpenses = prevTransactions.Where(t => t.Type == TransactionType.Расход).Sum(t => t.Amount);
                decimal prevProfit = prevIncome - prevExpenses;

                IncomeChangeText.Text = GetChangeText((decimal)_currentReport.TotalIncome, prevIncome);
                IncomeChangeText.Visibility = Visibility.Visible;
                ExpensesChangeText.Text = GetChangeText((decimal)_currentReport.TotalExpenses, prevExpenses);
                ExpensesChangeText.Visibility = Visibility.Visible;
                ProfitChangeText.Text = GetChangeText((decimal)_currentReport.Profit, prevProfit);
                ProfitChangeText.Visibility = Visibility.Visible;
            }
            else
            {
                IncomeChangeText.Visibility = Visibility.Collapsed;
                ExpensesChangeText.Visibility = Visibility.Collapsed;
                ProfitChangeText.Visibility = Visibility.Collapsed;
            }

            // Получаем все бронирования за период
            var bookings = await _bookingService.GetBookingsByDateRangeAsync(_startDate, _endDate);
            var allBookingsList = bookings.ToList();

// Фильтруем активные бронирования (не отменённые)
            var activeBookings = allBookingsList.Where(b => b.Status != BookingStatus.Отменено).ToList();
            _currentBookingsList = activeBookings;
            BookingsCountText.Text = _currentBookingsList.Count.ToString();

// Транзакции остаются без изменений
            var transactions = await _financeService.GetTransactionsAsync(_startDate, _endDate);
            _currentTransactionsList = transactions.ToList();

// Далее везде используем _currentBookingsList (активные) для расчётов
            
            var totalRooms = (await _roomService.GetAllRoomsAsync()).Count();
            var daysSpan = (_endDate - _startDate).Days;
            if (daysSpan == 0) daysSpan = 1;

            var _maxPossibleRoomDays = totalRooms * daysSpan;
            var _occupiedRoomDays = activeBookings.Sum(b => (b.CheckOutDate - b.CheckInDate).Days);

            double occupancyPercent = _maxPossibleRoomDays > 0 ? (double)_occupiedRoomDays / _maxPossibleRoomDays * 100 : 0;
            OccupancyText.Text = $"{occupancyPercent:F1}%";

            await UpdateOccupancyChartAsync(activeBookings, _occupiedRoomDays, _maxPossibleRoomDays);
            
            UpdateIncomeChart();
            UpdateCumulativeChart();
            UpdateIncomeStructureChart();

            // Прибыльность номеров
            var rooms = await _roomService.GetAllRoomsAsync();
            var roomProfits = rooms.Select(room => new {
                Id = room.Id,   // добавлено
                Name = room.Name,
                Type = room.Type.ToString(),
                Income = (double)_currentBookingsList.Where(b => b.RoomId == room.Id).Sum(b => b.PaidAmount) +
                         (double)_currentTransactionsList.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Доход).Sum(t => t.Amount),
                Expenses = (double)_currentTransactionsList.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Расход).Sum(t => t.Amount),
                Profit = 0.0
            }).Select(r => new {
                r.Id, r.Name, r.Type,
                r.Income, r.Expenses,
                Profit = r.Income - r.Expenses
            }).ToList();
            RoomProfitGrid.ItemsSource = roomProfits;

            // ---- Топ клиенты (расширенная версия) ----
            var clientSpending = new Dictionary<int, (decimal spentOnRooms, decimal spentOnServices, int bookingsCount)>();

            // Сбор трат на бронирования (номера)
            foreach (var booking in _currentBookingsList)
            {
                if (!clientSpending.ContainsKey(booking.ClientId))
                    clientSpending[booking.ClientId] = (0, 0, 0);
                var current = clientSpending[booking.ClientId];
                clientSpending[booking.ClientId] = (current.spentOnRooms + booking.PaidAmount, current.spentOnServices, current.bookingsCount + 1);
            }

            // Сбор трат на услуги (транзакции доходов, привязанные к бронированиям)
            foreach (var tx in _currentTransactionsList.Where(t => t.BookingId.HasValue && t.Type == TransactionType.Доход && t.ServiceId.HasValue))
            {
                var booking = _currentBookingsList.FirstOrDefault(b => b.Id == tx.BookingId);
                if (booking != null && clientSpending.ContainsKey(booking.ClientId))
                {
                    var current = clientSpending[booking.ClientId];
                    clientSpending[booking.ClientId] = (current.spentOnRooms, current.spentOnServices + tx.Amount, current.bookingsCount);
                }
            }

            var allClients = await _clientService.GetAllClientsAsync();
            var clientsData = new List<ClientData>();

            foreach (var client in allClients)
            {
                if (!clientSpending.ContainsKey(client.Id)) continue;
                var stats = clientSpending[client.Id];
                var lastBooking = _currentBookingsList
                    .Where(b => b.ClientId == client.Id)
                    .OrderByDescending(b => b.CheckInDate)
                    .Select(b => b.CheckInDate)
                    .FirstOrDefault();

                clientsData.Add(new ClientData
                {
                    ClientId = client.Id,
                    FullName = client.FullName,
                    BookingsCount = stats.bookingsCount,
                    SpentOnRooms = stats.spentOnRooms,
                    SpentOnServices = stats.spentOnServices,
                    TotalSpent = stats.spentOnRooms + stats.spentOnServices,
                    AverageCheck = stats.bookingsCount > 0 ? (stats.spentOnRooms + stats.spentOnServices) / stats.bookingsCount : 0,
                    LastBookingDate = lastBooking
                });
            }

            _allClientsData = clientsData.OrderByDescending(c => c.TotalSpent).Take(10).ToList();
            TopClientsGrid.ItemsSource = _allClientsData;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Обработчики UI (кнопки, переключения)

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
        UpdateIncomeChart();
    }

    private async void ToggleExpenses_Click(object sender, RoutedEventArgs e)
    {
        _showExpenses = !_showExpenses;
        var btn = sender as Button;
        btn.Content = _showExpenses ? "Показать доходы" : "Показать расходы";
        UpdateIncomeChart();
    }

    private void SwitchOccupancyChart_Click(object sender, RoutedEventArgs e)
    {
        _showOccupancyByDay = !_showOccupancyByDay;
        SwitchOccupancyChartBtn.Content = _showOccupancyByDay ? "По пирогу" : "По дням";
        UpdateOccupancyChartAsync(_currentBookingsList, occupiedRoomDays, maxPossibleRoomDays).Wait();
    }

    #endregion

    #region Графики (OxyPlot)

    private async Task UpdateOccupancyChartAsync(IEnumerable<Booking> activeBookings, int occupiedRoomDays, int maxPossibleRoomDays)
{
    try
    {
        this.occupiedRoomDays = occupiedRoomDays;
        this.maxPossibleRoomDays = maxPossibleRoomDays;
        var occupancyModel = new PlotModel { Title = _showOccupancyByDay ? "Загрузка по дням" : "Загрузка отеля" };
        
        if (_showOccupancyByDay)
        {
            var totalRooms = (await _roomService.GetAllRoomsAsync()).Count();
            var occupancyByDay = new Dictionary<DateTime, double>();
            for (var date = _startDate.Date; date <= _endDate.Date; date = date.AddDays(1))
            {
                var occupiedRooms = activeBookings
                    .Where(b => b.CheckInDate <= date && b.CheckOutDate > date)
                    .Select(b => b.RoomId)
                    .Distinct()
                    .Count();
                occupancyByDay[date] = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;
            }
            
            var lineSeries = new LineSeries
            {
                Title = "Загрузка",
                Color = OxyColor.FromRgb(52, 152, 219),
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4
            };
            foreach (var day in occupancyByDay.OrderBy(k => k.Key))
                lineSeries.Points.Add(new DataPoint(day.Key.ToOADate(), day.Value));
            occupancyModel.Series.Add(lineSeries);
            occupancyModel.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Дата", StringFormat = "dd.MM" });
            occupancyModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Загрузка (%)", Minimum = 0, Maximum = 100 });
        }
        else
        {
            if (maxPossibleRoomDays == 0)
                occupancyModel.Title = "Нет данных";
            else
            {
                var occupancy = (double)occupiedRoomDays / maxPossibleRoomDays * 100;
                occupancyModel.Title = $"Загрузка: {occupancy:F1}%";
                var pieSeries = new PieSeries
                {
                    StrokeThickness = 2,
                    InsideLabelPosition = 0.6,
                    InsideLabelColor = OxyColors.White,
                    OutsideLabelFormat = "{0}: {1:F1}%"
                };
                pieSeries.Slices.Add(new PieSlice("Занято", occupiedRoomDays) { Fill = OxyColor.FromRgb(52, 152, 219) });
                pieSeries.Slices.Add(new PieSlice("Свободно", Math.Max(0, maxPossibleRoomDays - occupiedRoomDays)) { Fill = OxyColor.FromRgb(149, 165, 166) });
                occupancyModel.Series.Add(pieSeries);
            }
        }
        OccupancyChart.Model = occupancyModel;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"UpdateOccupancyChart error: {ex.Message}");
    }
}

    private async void UpdateIncomeChart()
    {
        try
        {
            var startDate = _startDate.Date;
            var endDate = _endDate.Date.AddDays(1).AddSeconds(-1);
            string seriesTitle = _showExpenses ? "Расходы" : "Доходы";
            string modelTitle = _showExpenses ? 
                (_showIncomeByDay ? "Расходы по дням" : "Расходы по месяцам") :
                (_showIncomeByDay ? "Доходы по дням" : "Доходы по месяцам");
            
            var incomeModel = new PlotModel { Title = modelTitle };
            var incomeSeries = new BarSeries 
            { 
                Title = seriesTitle,
                FillColor = _showExpenses ? OxyColor.FromRgb(231, 76, 60) : OxyColor.FromRgb(39, 174, 96)
            };
            
            if (_showIncomeByDay)
            {
                var periodData = _showExpenses ?
                    _currentTransactionsList.Where(t => t.Type == TransactionType.Расход).GroupBy(t => t.TransactionDate.Date).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)) :
                    _currentTransactionsList.Where(t => t.Type == TransactionType.Доход).GroupBy(t => t.TransactionDate.Date).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
                var allDays = periodData.Keys.Where(d => d >= startDate && d <= endDate).OrderBy(d => d).Take(31);
                foreach (var day in allDays)
                    incomeSeries.Items.Add(new BarItem { Value = (double)periodData[day] });
                
                var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Дата" };
                foreach (var day in allDays)
                    categoryAxis.Labels.Add(day.ToString("dd.MM"));
                incomeModel.Axes.Add(categoryAxis);
                var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = $"Сумма ({AppConstants.Currency})", StringFormat = "N0" };
                incomeModel.Axes.Add(valueAxis);
            }
            else
            {
                var periodData = _showExpenses ?
                    _currentTransactionsList.Where(t => t.Type == TransactionType.Расход).GroupBy(t => t.TransactionDate.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)) :
                    _currentTransactionsList.Where(t => t.Type == TransactionType.Доход).GroupBy(t => t.TransactionDate.ToString("yyyy-MM")).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
                var months = periodData.OrderBy(m => m.Key).Select(m => m.Key).ToList();
                foreach (var month in months)
                    incomeSeries.Items.Add(new BarItem { Value = (double)periodData[month] });
                
                var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Месяц" };
                string[] monthNames = { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };
                foreach (var month in months)
                {
                    var monthIndex = int.Parse(month.Split('-')[1]) - 1;
                    categoryAxis.Labels.Add(monthNames[monthIndex]);
                }
                incomeModel.Axes.Add(categoryAxis);
                var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = $"Сумма ({AppConstants.Currency})", StringFormat = "N0" };
                incomeModel.Axes.Add(valueAxis);
            }
            incomeModel.Series.Add(incomeSeries);
            IncomeChart.Model = incomeModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateIncomeChart error: {ex.Message}");
        }
    }

    private void UpdateCumulativeChart()
    {
        try
        {
            var dailyIncome = _currentTransactionsList
                .Where(t => t.Type == TransactionType.Доход)
                .GroupBy(t => t.TransactionDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => (double)g.Sum(t => t.Amount))
                .ToList();
            var cumulative = new List<DataPoint>();
            double sum = 0;
            for (int i = 0; i < dailyIncome.Count; i++)
            {
                sum += dailyIncome[i];
                cumulative.Add(new DataPoint(i + 1, sum));
            }
            var model = new PlotModel { Title = "Накопленный доход" };
            var lineSeries = new LineSeries
            {
                Title = $"Доход накопленным итогом ({AppConstants.Currency})",
                Color = OxyColor.FromRgb(52, 152, 219),
                StrokeThickness = 2
            };
            foreach (var point in cumulative)
                lineSeries.Points.Add(point);
            model.Series.Add(lineSeries);
            
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Дни (от начала периода)" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = $"Накопленный доход ({AppConstants.Currency})", StringFormat = "N0" });
            
            CumulativeIncomeChart.Model = model;
        }
        catch { }
    }

    private void UpdateIncomeStructureChart()
    {
        try
        {
            var incomeByCategory = _currentTransactionsList
                .Where(t => t.Type == TransactionType.Доход)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key.ToString(), Amount = (double)g.Sum(t => t.Amount) })
                .Where(x => x.Amount > 0)
                .ToList();
            
            var model = new PlotModel { Title = "Структура доходов" };
            var pieSeries = new PieSeries
            {
                StrokeThickness = 1,
                InsideLabelPosition = 0.6,
                InsideLabelColor = OxyColors.White,
                OutsideLabelFormat = "{0}: {1:F1}%",
                AngleSpan = 360,
                StartAngle = 0
            };
            
            double total = incomeByCategory.Sum(x => x.Amount);
            foreach (var item in incomeByCategory)
            {
                double percent = total > 0 ? (item.Amount / total * 100) : 0;
                pieSeries.Slices.Add(new PieSlice($"{item.Category}\n{percent:F1}%", item.Amount));
            }
            model.Series.Add(pieSeries);
            IncomeStructureChart.Model = model;
        }
        catch { }
    }

    #endregion

    #region Экспорт (Excel, PDF, PNG)

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
        {
            MessageBox.Show("Недостаточно прав для экспорта отчётов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var dialog = new SaveFileDialog { Filter = "Excel files (*.xlsx)|*.xlsx", DefaultExt = "xlsx", FileName = $"HotelReport_{DateTime.Now:yyyyMMdd}" };
            if (dialog.ShowDialog() == true)
            {
                _excelExporter = new ExcelExporter(_financeService, _roomService, _clientService, _bookingService, _serviceService, _expenseService);
                var selectedItem = ReportTemplateComboBox.SelectedItem as ComboBoxItem;
                string template = selectedItem?.Tag?.ToString() ?? "Full";
                await _excelExporter.ExportWithTemplateAsync(_startDate, _endDate, dialog.FileName, template, _compareWithPrevious);
                SaveLastExportPath(dialog.FileName);
                await _logService.LogAsync(LogLevel.Средние, $"Создан отчёт Excel: {dialog.FileName} (шаблон: {template})", "ReportsView");
                MessageBox.Show($"Отчёт сохранён: {dialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Reports))
        {
            MessageBox.Show("Недостаточно прав для экспорта PDF!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var dialog = new SaveFileDialog { Filter = "PDF files (*.pdf)|*.pdf", DefaultExt = "pdf", FileName = $"HotelReport_{DateTime.Now:yyyyMMdd}" };
            if (dialog.ShowDialog() == true)
            {
                MessageBox.Show("Функция экспорта PDF в разработке.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
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
            var dialog = new SaveFileDialog { Filter = "PNG Image|*.png", DefaultExt = "png", FileName = "HotelCharts" };
            if (dialog.ShowDialog() == true)
            {
                PlotModel? occupancyModel = null, incomeModel = null;
                await Dispatcher.InvokeAsync(() => { occupancyModel = OccupancyChart.Model; incomeModel = IncomeChart.Model; });
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
                if (savedPaths.Count > 0) SaveLastExportPath(savedPaths[0]);
                await _logService.LogAsync(LogLevel.Средние, $"Сохранены графики: {string.Join(", ", savedPaths)}", "ReportsView");
                MessageBox.Show("Графики сохранены!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    #endregion

    private void RoomProfitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var selected = RoomProfitGrid.SelectedItem;
        if (selected != null)
        {
            dynamic room = selected;
            int roomId = room.Id;
            (Application.Current.MainWindow as MainWindow)?.NavigateToRooms(roomId);
        }
    }

    private void TopClientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TopClientsGrid.SelectedItem is ClientData client)
        {
            (Application.Current.MainWindow as MainWindow)?.NavigateToClients(client.ClientId);
        }
    }
    
    #region Вспомогательный класс для данных клиента

    private class ClientData
    {
        public int ClientId { get; set; }
        public string FullName { get; set; } = "";
        public int BookingsCount { get; set; }
        public decimal SpentOnRooms { get; set; }
        public decimal SpentOnServices { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageCheck { get; set; }
        public DateTime? LastBookingDate { get; set; }
    }

    #endregion
}
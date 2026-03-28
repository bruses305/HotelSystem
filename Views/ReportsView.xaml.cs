using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using HotelSystem.Services;
using HotelSystem.Helpers;
using Microsoft.Win32;
using System.IO;
using HotelSystem.Models.Entities;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;

namespace HotelSystem.Views;

public partial class ReportsView : Page
{
    private readonly IFinanceService _financeService;
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    private readonly ILogService _logService;
    
    private DateTime _startDate;
    private DateTime _endDate;
    private FinanceReport? _currentReport;
    private string _lastExportPath = "";
    private bool _showIncomeByDay = false; // false = по месяцам, true = по дням
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
        
        StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
        EndDatePicker.SelectedDate = DateTime.Today;
        
        _startDate = StartDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
        _endDate = EndDatePicker.SelectedDate ?? DateTime.Today;
        
        // Загружаем путь к последнему экспорту
        LoadLastExportPath();
        
        LoadReportAsync();
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
        catch { }
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
        catch { }
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
            // Включаем весь выбранный день
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
            
            // Транзакции для расчёта прибыльности номеров
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

            // График доходов - выносим в отдельный метод
            UpdateIncomeChart();

            // Прибыльность номеров - рассчитываем на основе реальных данных
            var rooms = await _roomService.GetAllRoomsAsync();
            var roomProfits = new List<object>();
            
            foreach (var room in rooms)
            {
                // Доход от бронирований этого номера
                var roomBookings = bookingsList.Where(b => b.RoomId == room.Id);
                var roomIncomeBookings = (double)roomBookings.Sum(b => b.PaidAmount);
                
                // Доход от услуг этого номера
                var roomServiceTxs = transactionsList
                    .Where(t => t.RoomId == room.Id && t.Type == TransactionType.Income);
                var roomIncomeServices = (double)roomServiceTxs.Sum(t => t.Amount);
                
                // Расходы этого номера
                var roomExpenses = transactionsList
                    .Where(t => t.RoomId == room.Id && t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);
                
                var totalIncome = roomIncomeBookings + roomIncomeServices;
                var totalExpenses = (double)roomExpenses;
                var profit = totalIncome - totalExpenses;

                roomProfits.Add(new 
                {
                    Name = room.Name,
                    Type = room.Type.ToString(),
                    Income = totalIncome,
                    Expenses = totalExpenses,
                    Profit = profit
                });
            }
            RoomProfitGrid.ItemsSource = roomProfits;

            // Топ клиентов за выбранный период
            var clientSpending = new Dictionary<int, (decimal spentOnRooms, decimal spentOnServices, int bookingsCount)>();
            
            // Считаем траты на номера по бронированиям за период
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
            
            // Считаем траты на услуги по транзакциям за период
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
                .Select(c => new 
                {
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
        LoadReportAsync();
    }

    private void SwitchChart_Click(object sender, RoutedEventArgs e)
    {
        _showIncomeByDay = !_showIncomeByDay;
        SwitchChartBtn.Content = _showIncomeByDay ? "По месяцам" : "По дням";
        IncomeChartTitle.Text = _showIncomeByDay ? "Доходы по дням" : "Доходы по месяцам";
        
        // Перестраиваем только график доходов
        UpdateIncomeChart();
    }

    private async void UpdateIncomeChart()
    {
        try
        {
            if (_currentReport == null) return;
            
            var startDate = _startDate.Date;
            var endDate = _endDate.Date.AddDays(1).AddSeconds(-1);
            
            var allBookings = await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate);
            var allBookingsList = allBookings.ToList();
            
            // График доходов по месяцам или дням
            var incomeModel = new PlotModel { Title = _showIncomeByDay ? "Доходы по дням" : "Доходы по месяцам" };
            var incomeSeries = new BarSeries { FillColor = OxyColor.FromRgb(39, 174, 96) };
            
            if (_showIncomeByDay)
            {
                // Доходы по дням
                var bookingIncomeByDay = allBookingsList
                    .Where(b => b.PaidAmount > 0)
                    .GroupBy(b => b.CreatedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.PaidAmount));
                
                var txIncomeByDay = _currentReport.Transactions
                    .Where(t => t.Type == TransactionType.Income)
                    .GroupBy(t => t.TransactionDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
                
                var allDays = bookingIncomeByDay.Keys.Union(txIncomeByDay.Keys)
                    .Where(d => d >= startDate && d <= endDate)
                    .OrderBy(d => d).Take(31);
                
                foreach (var day in allDays)
                {
                    decimal dayTotal = 0;
                    if (bookingIncomeByDay.ContainsKey(day)) dayTotal += bookingIncomeByDay[day];
                    if (txIncomeByDay.ContainsKey(day)) dayTotal += txIncomeByDay[day];
                    incomeSeries.Items.Add(new BarItem { Value = (double)dayTotal });
                }
                
                var categoryAxis = new OxyPlot.Axes.CategoryAxis { Position = OxyPlot.Axes.AxisPosition.Bottom };
                foreach (var day in allDays)
                {
                    categoryAxis.Labels.Add(day.ToString("dd.MM"));
                }
                incomeModel.Axes.Add(categoryAxis);
            }
            else
            {
                // Доходы по месяцам
                foreach (var month in _currentReport.IncomeByMonth.OrderBy(m => m.Key))
                {
                    incomeSeries.Items.Add(new BarItem { Value = (double)month.Value });
                }
                
                var categoryAxis = new OxyPlot.Axes.CategoryAxis { Position = OxyPlot.Axes.AxisPosition.Bottom };
                foreach (var month in _currentReport.IncomeByMonth.OrderBy(m => m.Key))
                {
                    categoryAxis.Labels.Add(month.Key);
                }
                incomeModel.Axes.Add(categoryAxis);
            }
            
            incomeModel.Series.Add(incomeSeries);
            IncomeChart.Model = incomeModel;
        }
        catch { }
    }

    private async void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
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
                using var workbook = new XLWorkbook();
                
                // Получаем данные для всех вкладок
                var startDate = _startDate.Date;
                var endDate = _endDate.Date.AddDays(1).AddSeconds(-1);
                
                var allBookings = await _bookingService.GetBookingsByDateRangeAsync(startDate, endDate);
                var allBookingsList = allBookings.ToList();
                var allClients = (await _clientService.GetAllClientsAsync()).ToList();
                var allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
                var allServices = (await _serviceService.GetAllServicesAsync()).ToList();
                var transactions = await _financeService.GetTransactionsAsync(startDate, endDate);
                var transactionsList = transactions.ToList();

                // Общий доход = оплаты бронирований + доходы от услуг
                var totalBookingPayments = allBookingsList.Sum(b => b.PaidAmount);
                var totalServiceIncome = transactionsList
                    .Where(t => t.Type == TransactionType.Income && t.Category == TransactionCategory.AdditionalService)
                    .Sum(t => t.Amount);
                var totalIncome = totalBookingPayments + totalServiceIncome;
                
                var totalExpenses = transactionsList
                    .Where(t => t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount);
                
                var profit = totalIncome - totalExpenses;

                // ==================== Лист "Финансы" ====================
                var financeSheet = workbook.Worksheets.Add("Финансы");
                financeSheet.Cell(1, 1).Value = "Финансовый отчёт";
                financeSheet.Cell(1, 1).Style.Font.Bold = true;
                financeSheet.Cell(1, 1).Style.Font.FontSize = 14;
                financeSheet.Range(1, 1, 1, 2).Merge();
                
                financeSheet.Cell(3, 1).Value = "Период:";
                financeSheet.Cell(3, 2).Value = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
                
                financeSheet.Cell(5, 1).Value = "Показатель";
                financeSheet.Cell(5, 2).Value = "Сумма";
                financeSheet.Range(5, 1, 5, 2).Style.Font.Bold = true;
                
                financeSheet.Cell(6, 1).Value = "Доходы от бронирований";
                financeSheet.Cell(6, 2).Value = (double)totalBookingPayments;
                financeSheet.Cell(6, 2).Style.NumberFormat.Format = "#,##0";
                
                financeSheet.Cell(7, 1).Value = "Доходы от услуг";
                financeSheet.Cell(7, 2).Value = (double)totalServiceIncome;
                financeSheet.Cell(7, 2).Style.NumberFormat.Format = "#,##0";
                
                financeSheet.Cell(8, 1).Value = "Общие доходы";
                financeSheet.Cell(8, 2).Value = (double)totalIncome;
                financeSheet.Cell(8, 2).Style.Font.Bold = true;
                financeSheet.Cell(8, 2).Style.NumberFormat.Format = "#,##0";
                
                financeSheet.Cell(10, 1).Value = "Общие расходы";
                financeSheet.Cell(10, 2).Value = (double)totalExpenses;
                financeSheet.Cell(10, 2).Style.NumberFormat.Format = "#,##0";
                
                financeSheet.Cell(11, 1).Value = "Прибыль";
                financeSheet.Cell(11, 2).Value = (double)profit;
                financeSheet.Cell(11, 2).Style.Font.Bold = true;
                financeSheet.Cell(11, 2).Style.NumberFormat.Format = "#,##0";
                
                financeSheet.Columns().AdjustToContents();

                // ==================== Лист "Все операции" ====================
                var allOpsSheet = workbook.Worksheets.Add("Все операции");
                allOpsSheet.Cell(1, 1).Value = "Полный журнал операций";
                allOpsSheet.Cell(1, 1).Style.Font.Bold = true;
                allOpsSheet.Cell(1, 1).Style.Font.FontSize = 14;
                allOpsSheet.Range(1, 1, 1, 8).Merge();
                
                AddHeaderRow(allOpsSheet, 3, "Дата", "Тип", "Категория", "Номер", "Клиент", "Описание", "Сумма");
                
                int row = 4;
                bool alternate = false;
                
                // Оплаты бронирований (исключаем транзакции с категорией Booking - они дублируют PaidAmount)
                foreach (var booking in allBookingsList.Where(b => b.PaidAmount > 0))
                {
                    var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                    var room = allRooms.FirstOrDefault(r => r.Id == booking.RoomId);
                    
                    AddDataRow(allOpsSheet, row, alternate,
                        booking.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        "Доход", "Оплата бронирования",
                        room?.Name ?? $"№{booking.RoomId}",
                        client?.FullName ?? "Клиент",
                        $"Бронирование #{booking.Id}",
                        (double)booking.PaidAmount, true);
                    row++;
                    alternate = !alternate;
                }
                
                // Транзакции (исключаем категорию Booking - она уже учтена в PaidAmount)
                foreach (var tx in transactionsList.Where(t => t.Category != TransactionCategory.Booking).OrderBy(t => t.TransactionDate))
                {
                    var room = tx.RoomId.HasValue ? allRooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
                    var service = tx.ServiceId.HasValue ? allServices.FirstOrDefault(s => s.Id == tx.ServiceId) : null;
                    string clientName = "";
                    string category = tx.Category.ToString();
                    
                    // Конкретизируем категорию
                    if (tx.Category == TransactionCategory.AdditionalService && service != null)
                    {
                        category = $"Услуга: {service.Name}";
                    }
                    else if (tx.Category == TransactionCategory.Utilities)
                    {
                        string desc = tx.Description?.ToLower() ?? "";
                        if (desc.Contains("вода")) category = "Расход: Вода";
                        else if (desc.Contains("электричеств")) category = "Расход: Электричество";
                        else if (desc.Contains("интернет")) category = "Расход: Интернет";
                        else category = "Расход: Коммунальные услуги";
                    }
                    else if (tx.Category == TransactionCategory.Maintenance)
                    {
                        string desc = tx.Description?.ToLower() ?? "";
                        if (desc.Contains("уборка")) category = "Расход: Уборка";
                        else if (desc.Contains("ремонт")) category = "Расход: Ремонт";
                        else category = "Расход: Обслуживание";
                    }
                    else if (tx.Category == TransactionCategory.Salary)
                    {
                        category = "Расход: Зарплата";
                    }
                    
                    if (tx.BookingId.HasValue)
                    {
                        var booking = allBookingsList.FirstOrDefault(b => b.Id == tx.BookingId);
                        if (booking != null)
                        {
                            var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                            clientName = client?.FullName ?? "";
                        }
                    }
                    
                    bool isIncome = tx.Type == TransactionType.Income;
                    AddDataRow(allOpsSheet, row, alternate,
                        tx.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                        isIncome ? "Доход" : "Расход",
                        category,
                        service?.Name ?? room?.Name ?? (tx.RoomId.HasValue ? $"№{tx.RoomId}" : "-"),
                        clientName,
                        tx.Description ?? "-",
                        (double)tx.Amount, isIncome);
                    row++;
                    alternate = !alternate;
                }
                
                allOpsSheet.Columns().AdjustToContents();
                
                // Включаем фильтры
                allOpsSheet.Range(3, 1, row - 1, 8).SetAutoFilter();

                // ==================== Лист "Только доходы" ====================
                var incomeOnlySheet = workbook.Worksheets.Add("Только доходы");
                incomeOnlySheet.Cell(1, 1).Value = "Все доходы";
                incomeOnlySheet.Cell(1, 1).Style.Font.Bold = true;
                incomeOnlySheet.Cell(1, 1).Style.Font.FontSize = 14;
                incomeOnlySheet.Range(1, 1, 1, 7).Merge();
                
                AddHeaderRow(incomeOnlySheet, 3, "Дата", "Категория", "Номер", "Клиент", "Описание", "Сумма");
                
                row = 4;
                decimal totalIncomeOnly = 0;
                
                foreach (var booking in allBookingsList.Where(b => b.PaidAmount > 0))
                {
                    var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                    var room = allRooms.FirstOrDefault(r => r.Id == booking.RoomId);
                    
                    AddDataRowSimple(incomeOnlySheet, row, row % 2 == 0,
                        booking.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        "Booking",
                        room?.Name ?? $"№{booking.RoomId}",
                        client?.FullName ?? "Клиент",
                        $"Бронирование #{booking.Id}",
                        (double)booking.PaidAmount);
                    totalIncomeOnly += booking.PaidAmount;
                    row++;
                }
                
                foreach (var tx in transactionsList.Where(t => t.Type == TransactionType.Income).OrderBy(t => t.TransactionDate))
                {
                    var room = tx.RoomId.HasValue ? allRooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
                    var service = tx.ServiceId.HasValue ? allServices.FirstOrDefault(s => s.Id == tx.ServiceId) : null;
                    string clientName = "";
                    
                    if (tx.BookingId.HasValue)
                    {
                        var booking = allBookingsList.FirstOrDefault(b => b.Id == tx.BookingId);
                        if (booking != null)
                        {
                            var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                            clientName = client?.FullName ?? "";
                        }
                    }
                    
                    AddDataRowSimple(incomeOnlySheet, row, row % 2 == 0,
                        tx.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                        tx.Category.ToString(),
                        service?.Name ?? room?.Name ?? "-",
                        clientName,
                        tx.Description ?? "-",
                        (double)tx.Amount);
                    totalIncomeOnly += tx.Amount;
                    row++;
                }
                
                incomeOnlySheet.Cell(row, 1).Value = "ИТОГО:";
                incomeOnlySheet.Cell(row, 1).Style.Font.Bold = true;
                incomeOnlySheet.Cell(row, 6).Value = (double)totalIncomeOnly;
                incomeOnlySheet.Cell(row, 6).Style.Font.Bold = true;
                incomeOnlySheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                
                incomeOnlySheet.Columns().AdjustToContents();
                incomeOnlySheet.Range(3, 1, row - 1, 7).SetAutoFilter();

                // ==================== Лист "Только расходы" ====================
                var expenseOnlySheet = workbook.Worksheets.Add("Только расходы");
                expenseOnlySheet.Cell(1, 1).Value = "Все расходы";
                expenseOnlySheet.Cell(1, 1).Style.Font.Bold = true;
                expenseOnlySheet.Cell(1, 1).Style.Font.FontSize = 14;
                expenseOnlySheet.Range(1, 1, 1, 7).Merge();
                
                AddHeaderRow(expenseOnlySheet, 3, "Дата", "Категория", "Номер", "Описание", "Количество", "Сумма");
                
                row = 4;
                decimal totalExpensesOnly = 0;
                
                foreach (var tx in transactionsList.Where(t => t.Type == TransactionType.Expense).OrderBy(t => t.TransactionDate))
                {
                    var room = tx.RoomId.HasValue ? allRooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
                    
                    AddDataRowSimple(expenseOnlySheet, row, row % 2 == 0,
                        tx.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                        tx.Category.ToString(),
                        room?.Name ?? (tx.RoomId.HasValue ? $"№{tx.RoomId}" : "-"),
                        tx.Description ?? "-",
                        tx.Quantity > 0 ? tx.Quantity.ToString() : "-",
                        (double)tx.Amount);
                    totalExpensesOnly += tx.Amount;
                    row++;
                }
                
                expenseOnlySheet.Cell(row, 1).Value = "ИТОГО:";
                expenseOnlySheet.Cell(row, 1).Style.Font.Bold = true;
                expenseOnlySheet.Cell(row, 6).Value = (double)totalExpensesOnly;
                expenseOnlySheet.Cell(row, 6).Style.Font.Bold = true;
                expenseOnlySheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                
                expenseOnlySheet.Columns().AdjustToContents();
                expenseOnlySheet.Range(3, 1, row - 1, 7).SetAutoFilter();

                // ==================== Лист "Доходы по типам" ====================
                var incomeTypesSheet = workbook.Worksheets.Add("Доходы по типам");
                incomeTypesSheet.Cell(1, 1).Value = "Доходы по типам";
                incomeTypesSheet.Cell(1, 1).Style.Font.Bold = true;
                incomeTypesSheet.Cell(1, 1).Style.Font.FontSize = 14;
                incomeTypesSheet.Range(1, 1, 1, 3).Merge();
                
                AddHeaderRow(incomeTypesSheet, 3, "Тип дохода", "Количество операций", "Сумма");
                
                row = 4;
                
                // Оплаты бронирований
                var bookingPayments = allBookingsList.Where(b => b.PaidAmount > 0).ToList();
                if (bookingPayments.Any())
                {
                    incomeTypesSheet.Cell(row, 1).Value = "Оплата бронирований";
                    incomeTypesSheet.Cell(row, 2).Value = bookingPayments.Count;
                    incomeTypesSheet.Cell(row, 3).Value = (double)bookingPayments.Sum(b => b.PaidAmount);
                    incomeTypesSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                // Группировка доходов по услугам (более конкретно)
                var incomeByService = transactionsList
                    .Where(t => t.Type == TransactionType.Income && t.ServiceId.HasValue)
                    .GroupBy(t => t.ServiceId)
                    .Select(g => new { 
                        ServiceId = g.Key,
                        ServiceName = allServices.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Услуга",
                        Count = g.Sum(x => x.Quantity > 0 ? x.Quantity : 1), 
                        Sum = g.Sum(x => x.Amount) 
                    });
                
                foreach (var item in incomeByService)
                {
                    incomeTypesSheet.Cell(row, 1).Value = $"Услуга: {item.ServiceName}";
                    incomeTypesSheet.Cell(row, 2).Value = item.Count;
                    incomeTypesSheet.Cell(row, 3).Value = (double)item.Sum;
                    incomeTypesSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                incomeTypesSheet.Cell(row, 1).Value = "ИТОГО:";
                incomeTypesSheet.Cell(row, 1).Style.Font.Bold = true;
                incomeTypesSheet.Cell(row, 3).Value = (double)totalIncome;
                incomeTypesSheet.Cell(row, 3).Style.Font.Bold = true;
                incomeTypesSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                
                incomeTypesSheet.Columns().AdjustToContents();
                incomeTypesSheet.Range(3, 1, row - 1, 3).SetAutoFilter();

                // ==================== Лист "Расходы по типам" ====================
                var expenseTypesSheet = workbook.Worksheets.Add("Расходы по типам");
                expenseTypesSheet.Cell(1, 1).Value = "Расходы по типам";
                expenseTypesSheet.Cell(1, 1).Style.Font.Bold = true;
                expenseTypesSheet.Cell(1, 1).Style.Font.FontSize = 14;
                expenseTypesSheet.Range(1, 1, 1, 3).Merge();
                
                AddHeaderRow(expenseTypesSheet, 3, "Тип расхода", "Количество операций", "Сумма");
                
                row = 4;
                
                // Группировка расходов по описанию (более конкретно)
                var expensesByDescription = transactionsList
                    .Where(t => t.Type == TransactionType.Expense)
                    .GroupBy(t => {
                        // Конкретизируем тип расхода по категории и описанию
                        string desc = t.Description?.ToLower() ?? "";
                        
                        if (t.Category == TransactionCategory.Utilities)
                        {
                            if (desc.Contains("вода")) return "Расход: Вода";
                            if (desc.Contains("электричеств")) return "Расход: Электричество";
                            if (desc.Contains("интернет")) return "Расход: Интернет";
                            return "Расход: Коммунальные услуги";
                        }
                        if (t.Category == TransactionCategory.Maintenance)
                        {
                            if (desc.Contains("уборка")) return "Расход: Уборка";
                            if (desc.Contains("ремонт")) return "Расход: Ремонт";
                            return "Расход: Обслуживание";
                        }
                        if (t.Category == TransactionCategory.Salary)
                        {
                            return "Расход: Зарплата";
                        }
                        if (t.Category == TransactionCategory.Purchase)
                        {
                            return "Расход: Закупки";
                        }
                        return t.Category.ToString();
                    })
                    .Select(g => new { Category = g.Key, Count = g.Count(), Sum = g.Sum(t => t.Amount) });
                
                foreach (var item in expensesByDescription)
                {
                    expenseTypesSheet.Cell(row, 1).Value = item.Category;
                    expenseTypesSheet.Cell(row, 2).Value = item.Count;
                    expenseTypesSheet.Cell(row, 3).Value = (double)item.Sum;
                    expenseTypesSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                expenseTypesSheet.Cell(row, 1).Value = "ИТОГО:";
                expenseTypesSheet.Cell(row, 1).Style.Font.Bold = true;
                expenseTypesSheet.Cell(row, 3).Value = (double)totalExpenses;
                expenseTypesSheet.Cell(row, 3).Style.Font.Bold = true;
                expenseTypesSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                
                expenseTypesSheet.Columns().AdjustToContents();
                expenseTypesSheet.Range(3, 1, row - 1, 3).SetAutoFilter();

                // ==================== Лист "Доходы по дням" ====================
                var incomeByDaySheet = workbook.Worksheets.Add("Доходы по дням");
                incomeByDaySheet.Cell(1, 1).Value = "Доходы по дням";
                incomeByDaySheet.Cell(1, 1).Style.Font.Bold = true;
                incomeByDaySheet.Cell(1, 1).Style.Font.FontSize = 14;
                incomeByDaySheet.Range(1, 1, 1, 2).Merge();
                
                AddHeaderRow(incomeByDaySheet, 3, "Дата", "Сумма");
                
                row = 4;
                
                // Оплаты бронирований по дням
                var bookingIncomeByDay = allBookingsList
                    .Where(b => b.PaidAmount > 0)
                    .GroupBy(b => b.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Sum = g.Sum(b => b.PaidAmount) })
                    .ToDictionary(x => x.Date, x => x.Sum);
                
                // Транзакции по дням
                var txIncomeByDay = transactionsList
                    .Where(t => t.Type == TransactionType.Income)
                    .GroupBy(t => t.TransactionDate.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
                
                // Объединяем
                var allDates = bookingIncomeByDay.Keys.Union(txIncomeByDay.Keys).OrderBy(d => d);
                
                foreach (var date in allDates)
                {
                    decimal dayTotal = 0;
                    if (bookingIncomeByDay.ContainsKey(date)) dayTotal += bookingIncomeByDay[date];
                    if (txIncomeByDay.ContainsKey(date)) dayTotal += txIncomeByDay[date];
                    
                    incomeByDaySheet.Cell(row, 1).Value = date.ToString("dd.MM.yyyy");
                    incomeByDaySheet.Cell(row, 2).Value = (double)dayTotal;
                    incomeByDaySheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                incomeByDaySheet.Cell(row, 1).Value = "ИТОГО:";
                incomeByDaySheet.Cell(row, 1).Style.Font.Bold = true;
                incomeByDaySheet.Cell(row, 2).Value = (double)totalIncome;
                incomeByDaySheet.Cell(row, 2).Style.Font.Bold = true;
                incomeByDaySheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                
                incomeByDaySheet.Columns().AdjustToContents();
                incomeByDaySheet.Range(3, 1, row - 1, 2).SetAutoFilter();

                // ==================== Лист "Доходы по месяцам" ====================
                var incomeByMonthSheet = workbook.Worksheets.Add("Доходы по месяцам");
                incomeByMonthSheet.Cell(1, 1).Value = "Доходы по месяцам";
                incomeByMonthSheet.Cell(1, 1).Style.Font.Bold = true;
                incomeByMonthSheet.Cell(1, 1).Style.Font.FontSize = 14;
                incomeByMonthSheet.Range(1, 1, 1, 2).Merge();
                
                AddHeaderRow(incomeByMonthSheet, 3, "Месяц", "Сумма");
                
                row = 4;
                
                // Оплаты бронирований по месяцам
                var bookingIncomeByMonth = allBookingsList
                    .Where(b => b.PaidAmount > 0)
                    .GroupBy(b => b.CreatedAt.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.PaidAmount));
                
                // Транзакции по месяцам
                var txIncomeByMonth = transactionsList
                    .Where(t => t.Type == TransactionType.Income)
                    .GroupBy(t => t.TransactionDate.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
                
                var allMonths = bookingIncomeByMonth.Keys.Union(txIncomeByMonth.Keys).OrderBy(m => m);
                
                foreach (var month in allMonths)
                {
                    decimal monthTotal = 0;
                    if (bookingIncomeByMonth.ContainsKey(month)) monthTotal += bookingIncomeByMonth[month];
                    if (txIncomeByMonth.ContainsKey(month)) monthTotal += txIncomeByMonth[month];
                    
                    var dateParts = month.Split('-');
                    var monthName = new DateTime(int.Parse(dateParts[0]), int.Parse(dateParts[1]), 1).ToString("MMMM yyyy");
                    
                    incomeByMonthSheet.Cell(row, 1).Value = monthName;
                    incomeByMonthSheet.Cell(row, 2).Value = (double)monthTotal;
                    incomeByMonthSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                incomeByMonthSheet.Cell(row, 1).Value = "ИТОГО:";
                incomeByMonthSheet.Cell(row, 1).Style.Font.Bold = true;
                incomeByMonthSheet.Cell(row, 2).Value = (double)totalIncome;
                incomeByMonthSheet.Cell(row, 2).Style.Font.Bold = true;
                incomeByMonthSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                
                incomeByMonthSheet.Columns().AdjustToContents();
                incomeByMonthSheet.Range(3, 1, row - 1, 2).SetAutoFilter();

                // ==================== Лист "Бронирования" ====================
                var bookingsSheet = workbook.Worksheets.Add("Бронирования");
                bookingsSheet.Cell(1, 1).Value = "Список бронирований";
                bookingsSheet.Cell(1, 1).Style.Font.Bold = true;
                bookingsSheet.Cell(1, 1).Style.Font.FontSize = 14;
                bookingsSheet.Range(1, 1, 1, 9).Merge();
                
                AddHeaderRow(bookingsSheet, 3, "ID", "Номер", "Клиент", "Заезд", "Выезд", "Статус", "Оплачено", "Сумма", "Дней");
                
                row = 4;
                foreach (var booking in allBookingsList)
                {
                    var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                    var room = allRooms.FirstOrDefault(r => r.Id == booking.RoomId);
                    
                    bookingsSheet.Cell(row, 1).Value = booking.Id;
                    bookingsSheet.Cell(row, 2).Value = room?.Name ?? $"№{booking.RoomId}";
                    bookingsSheet.Cell(row, 3).Value = client?.FullName ?? "Клиент";
                    bookingsSheet.Cell(row, 4).Value = booking.CheckInDate.ToString("dd.MM.yyyy");
                    bookingsSheet.Cell(row, 5).Value = booking.CheckOutDate.ToString("dd.MM.yyyy");
                    bookingsSheet.Cell(row, 6).Value = booking.Status.ToString();
                    bookingsSheet.Cell(row, 7).Value = (double)booking.PaidAmount;
                    bookingsSheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                    bookingsSheet.Cell(row, 8).Value = (double)booking.TotalPrice;
                    bookingsSheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                    bookingsSheet.Cell(row, 9).Value = booking.Days;
                    row++;
                }
                
                bookingsSheet.Columns().AdjustToContents();
                bookingsSheet.Range(3, 1, row - 1, 9).SetAutoFilter();

                // ==================== Лист "Номера" ====================
                var roomsSheet = workbook.Worksheets.Add("Номера");
                roomsSheet.Cell(1, 1).Value = "Прибыльность номеров";
                roomsSheet.Cell(1, 1).Style.Font.Bold = true;
                roomsSheet.Cell(1, 1).Style.Font.FontSize = 14;
                roomsSheet.Range(1, 1, 1, 6).Merge();
                
                AddHeaderRow(roomsSheet, 3, "Номер", "Тип", "Доход от бронирований", "Доход от услуг", "Расходы", "Прибыль");
                
                row = 4;
                foreach (var room in allRooms)
                {
                    var roomBookings = allBookingsList.Where(b => b.RoomId == room.Id);
                    var roomIncomeBookings = roomBookings.Sum(b => b.PaidAmount);
                    
                    var roomTx = transactionsList.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Income);
                    var roomIncomeServices = roomTx.Sum(t => t.Amount);
                    
                    var roomExpenses = transactionsList
                        .Where(t => t.RoomId == room.Id && t.Type == TransactionType.Expense)
                        .Sum(t => t.Amount);
                    
                    var roomProfit = roomIncomeBookings + roomIncomeServices - roomExpenses;
                    
                    roomsSheet.Cell(row, 1).Value = room.Name;
                    roomsSheet.Cell(row, 2).Value = room.Type.ToString();
                    roomsSheet.Cell(row, 3).Value = (double)roomIncomeBookings;
                    roomsSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    roomsSheet.Cell(row, 4).Value = (double)roomIncomeServices;
                    roomsSheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                    roomsSheet.Cell(row, 5).Value = (double)roomExpenses;
                    roomsSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    roomsSheet.Cell(row, 6).Value = (double)roomProfit;
                    roomsSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                roomsSheet.Columns().AdjustToContents();
                roomsSheet.Range(3, 1, row - 1, 6).SetAutoFilter();

                // ==================== Лист "Услуги" ====================
                var servicesSheet = workbook.Worksheets.Add("Услуги");
                servicesSheet.Cell(1, 1).Value = "Операции по услугам";
                servicesSheet.Cell(1, 1).Style.Font.Bold = true;
                servicesSheet.Cell(1, 1).Style.Font.FontSize = 14;
                servicesSheet.Range(1, 1, 1, 7).Merge();
                
                AddHeaderRow(servicesSheet, 3, "Дата", "Услуга", "Клиент", "Бронирование", "Количество", "Сумма");
                
                row = 4;
                decimal totalServiceRevenue = 0;
                
                var serviceTransactions = transactionsList
                    .Where(t => t.ServiceId.HasValue)
                    .OrderBy(t => t.TransactionDate);
                
                foreach (var tx in serviceTransactions)
                {
                    var service = allServices.FirstOrDefault(s => s.Id == tx.ServiceId);
                    string clientName = "";
                    
                    if (tx.BookingId.HasValue)
                    {
                        var booking = allBookingsList.FirstOrDefault(b => b.Id == tx.BookingId);
                        if (booking != null)
                        {
                            var client = allClients.FirstOrDefault(c => c.Id == booking.ClientId);
                            clientName = client?.FullName ?? "";
                        }
                    }
                    
                    servicesSheet.Cell(row, 1).Value = tx.TransactionDate.ToString("dd.MM.yyyy HH:mm");
                    servicesSheet.Cell(row, 2).Value = service?.Name ?? "Услуга";
                    servicesSheet.Cell(row, 3).Value = clientName;
                    servicesSheet.Cell(row, 4).Value = tx.BookingId.HasValue ? $"#{tx.BookingId}" : "-";
                    servicesSheet.Cell(row, 5).Value = tx.Quantity > 0 ? tx.Quantity : 1;
                    servicesSheet.Cell(row, 6).Value = (double)tx.Amount;
                    servicesSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                    
                    totalServiceRevenue += tx.Amount;
                    row++;
                }
                
                servicesSheet.Cell(row, 1).Value = "ИТОГО:";
                servicesSheet.Cell(row, 1).Style.Font.Bold = true;
                servicesSheet.Cell(row, 6).Value = (double)totalServiceRevenue;
                servicesSheet.Cell(row, 6).Style.Font.Bold = true;
                servicesSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                
                servicesSheet.Columns().AdjustToContents();
                servicesSheet.Range(3, 1, row - 1, 7).SetAutoFilter();

                // ==================== Лист "Прибыль по услугам" ====================
                var serviceProfitSheet = workbook.Worksheets.Add("Прибыль по услугам");
                serviceProfitSheet.Cell(1, 1).Value = "Прибыль по услугам";
                serviceProfitSheet.Cell(1, 1).Style.Font.Bold = true;
                serviceProfitSheet.Cell(1, 1).Style.Font.FontSize = 14;
                serviceProfitSheet.Range(1, 1, 1, 4).Merge();
                
                AddHeaderRow(serviceProfitSheet, 3, "Услуга", "Количество продаж", "Выручка", "Цена за единицу");
                
                row = 4;
                
                var serviceStats = transactionsList
                    .Where(t => t.ServiceId.HasValue && t.Type == TransactionType.Income)
                    .GroupBy(t => t.ServiceId)
                    .Select(g => new
                    {
                        ServiceId = g.Key,
                        ServiceName = allServices.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Услуга",
                        Count = g.Sum(t => t.Quantity > 0 ? t.Quantity : 1),
                        Revenue = g.Sum(t => t.Amount),
                        Price = g.First().Amount / (g.First().Quantity > 0 ? g.First().Quantity : 1)
                    })
                    .OrderByDescending(s => s.Revenue);
                
                foreach (var stat in serviceStats)
                {
                    serviceProfitSheet.Cell(row, 1).Value = stat.ServiceName;
                    serviceProfitSheet.Cell(row, 2).Value = stat.Count;
                    serviceProfitSheet.Cell(row, 3).Value = (double)stat.Revenue;
                    serviceProfitSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    serviceProfitSheet.Cell(row, 4).Value = (double)stat.Price;
                    serviceProfitSheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                    row++;
                }
                
                serviceProfitSheet.Cell(row, 1).Value = "ИТОГО:";
                serviceProfitSheet.Cell(row, 1).Style.Font.Bold = true;
                serviceProfitSheet.Cell(row, 3).Value = (double)totalServiceRevenue;
                serviceProfitSheet.Cell(row, 3).Style.Font.Bold = true;
                serviceProfitSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                
                serviceProfitSheet.Columns().AdjustToContents();
                serviceProfitSheet.Range(3, 1, row - 1, 4).SetAutoFilter();

                // ==================== Лист "Клиенты" ====================
                var clientsSheet = workbook.Worksheets.Add("Клиенты");
                clientsSheet.Cell(1, 1).Value = "Клиенты";
                clientsSheet.Cell(1, 1).Style.Font.Bold = true;
                clientsSheet.Cell(1, 1).Style.Font.FontSize = 14;
                clientsSheet.Range(1, 1, 1, 6).Merge();
                
                AddHeaderRow(clientsSheet, 3, "Клиент", "Бронирований", "Потрачено на номера", "Услуг куплено", "Потрачено на услуги", "Всего потрачено");
                
                row = 4;
                
                foreach (var client in allClients)
                {
                    var clientBookings = allBookingsList.Where(b => b.ClientId == client.Id);
                    var spentOnRooms = clientBookings.Sum(b => b.PaidAmount);
                    
                    var clientServiceTxs = transactionsList
                        .Where(t => t.BookingId.HasValue && 
                                   allBookingsList.Any(b => b.Id == t.BookingId && b.ClientId == client.Id))
                        .ToList();
                    
                    var servicesBought = clientServiceTxs.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
                    var spentOnServices = clientServiceTxs.Sum(t => t.Amount);
                    var totalSpent = spentOnRooms + spentOnServices;
                    
                    var bookingCount = clientBookings.Count();
                    
                    if (bookingCount > 0 || servicesBought > 0)
                    {
                        clientsSheet.Cell(row, 1).Value = client.FullName;
                        clientsSheet.Cell(row, 2).Value = bookingCount;
                        clientsSheet.Cell(row, 3).Value = (double)spentOnRooms;
                        clientsSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        clientsSheet.Cell(row, 4).Value = servicesBought;
                        clientsSheet.Cell(row, 5).Value = (double)spentOnServices;
                        clientsSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        clientsSheet.Cell(row, 6).Value = (double)totalSpent;
                        clientsSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        clientsSheet.Cell(row, 6).Style.Font.Bold = true;
                        row++;
                    }
                }
                
                clientsSheet.Columns().AdjustToContents();
                clientsSheet.Range(3, 1, row - 1, 6).SetAutoFilter();

                workbook.SaveAs(dialog.FileName);
                
                // Сохраняем путь и включаем кнопку
                SaveLastExportPath(dialog.FileName);
                
                // Логирование
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

    // Вспомогательные методы для Excel
    private void AddHeaderRow(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = headers[i];
            sheet.Cell(row, i + 1).Style.Font.Bold = true;
            sheet.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            sheet.Cell(row, i + 1).Style.Font.FontColor = XLColor.White;
            sheet.Cell(row, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }

    private void AddDataRow(IXLWorksheet sheet, int row, bool alternateRow, string date, string type, string category, 
        string room, string client, string description, double amount, bool isIncome)
    {
        var bgColor = alternateRow ? XLColor.FromHtml("#F8F9FA") : XLColor.White;
        
        sheet.Cell(row, 1).Value = date;
        sheet.Cell(row, 1).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 2).Value = type;
        sheet.Cell(row, 2).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 3).Value = category;
        sheet.Cell(row, 3).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 4).Value = room;
        sheet.Cell(row, 4).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 5).Value = client;
        sheet.Cell(row, 5).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 6).Value = description;
        sheet.Cell(row, 6).Style.Fill.BackgroundColor = bgColor;
        
        sheet.Cell(row, 7).Value = amount;
        sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(row, 7).Style.Fill.BackgroundColor = bgColor;
        
        if (isIncome)
            sheet.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
        else
            sheet.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
        
        // Обводка
        for (int i = 1; i <= 7; i++)
        {
            sheet.Cell(row, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }

    private void AddDataRowSimple(IXLWorksheet sheet, int row, bool alternateRow, params object[] values)
    {
        var bgColor = alternateRow ? XLColor.FromHtml("#F8F9FA") : XLColor.White;
        
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] is double d)
            {
                sheet.Cell(row, i + 1).Value = d;
                sheet.Cell(row, i + 1).Style.NumberFormat.Format = "#,##0";
            }
            else
            {
                sheet.Cell(row, i + 1).Value = values[i]?.ToString() ?? "";
            }
            sheet.Cell(row, i + 1).Style.Fill.BackgroundColor = bgColor;
            sheet.Cell(row, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i + 1).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }

    private async void ExportCharts_Click(object sender, RoutedEventArgs e)
    {
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
                // Получаем модели графиков в UI-потоке
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
                
                // Экспорт графика загрузки
                if (occupancyModel != null)
                {
                    var occPath = Path.Combine(basePath, $"{baseName}_Occupancy.png");
                    await ExportPlotModelToPngAsync(occupancyModel, occPath);
                    savedPaths.Add(occPath);
                }
                
                // Экспорт графика доходов
                if (incomeModel != null)
                {
                    var incPath = Path.Combine(basePath, $"{baseName}_Income.png");
                    await ExportPlotModelToPngAsync(incomeModel, incPath);
                    savedPaths.Add(incPath);
                }
                
                // Сохраняем путь к первому файлу для кнопки открытия
                if (savedPaths.Count > 0)
                {
                    SaveLastExportPath(savedPaths[0]);
                }
                
                // Логирование
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
    
    private Task ExportPlotModelToPngAsync(PlotModel model, string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                // Создаём новую модель для экспорта
                var exportModel = new PlotModel
                {
                    Title = model.Title,
                    Background = OxyColors.White,
                    PlotAreaBorderColor = OxyColors.Black,
                    TextColor = OxyColors.Black,
                    TitleColor = OxyColors.Black
                };
                
                // Копируем оси
                foreach (var axis in model.Axes)
                {
                    if (axis is OxyPlot.Axes.CategoryAxis catAxis)
                    {
                        var newAxis = new OxyPlot.Axes.CategoryAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            TitleFontSize = axis.TitleFontSize,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black,
                            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                            MajorGridlineStyle = LineStyle.Solid
                        };
                        foreach (var label in catAxis.Labels)
                        {
                            newAxis.Labels.Add(label);
                        }
                        exportModel.Axes.Add(newAxis);
                    }
                    else if (axis is OxyPlot.Axes.LinearAxis linAxis)
                    {
                        exportModel.Axes.Add(new OxyPlot.Axes.LinearAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            TitleFontSize = axis.TitleFontSize,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black,
                            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                            MajorGridlineStyle = LineStyle.Solid,
                            Minimum = linAxis.Minimum,
                            Maximum = linAxis.Maximum
                        });
                    }
                    else
                    {
                        exportModel.Axes.Add(new OxyPlot.Axes.CategoryAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black
                        });
                    }
                }
                
                // Копируем серии
                foreach (var series in model.Series)
                {
                    if (series is PieSeries pieSeries)
                    {
                        var newPie = new PieSeries
                        {
                            StrokeThickness = pieSeries.StrokeThickness,
                            InsideLabelPosition = pieSeries.InsideLabelPosition,
                            AngleSpan = pieSeries.AngleSpan,
                            StartAngle = pieSeries.StartAngle,
                            InsideLabelColor = OxyColors.Black,
                            OutsideLabelFormat = "{1}: {2}",
                            Stroke = OxyColors.White
                        };
                        foreach (var slice in pieSeries.Slices)
                        {
                            newPie.Slices.Add(new PieSlice(slice.Label, slice.Value) { Fill = slice.Fill });
                        }
                        exportModel.Series.Add(newPie);
                    }
                    else if (series is BarSeries barSeries)
                    {
                        var newBar = new BarSeries
                        {
                            FillColor = barSeries.FillColor,
                            StrokeColor = OxyColors.Black,
                            StrokeThickness = 1
                        };
                        foreach (var item in barSeries.Items)
                        {
                            newBar.Items.Add(new BarItem { Value = item.Value });
                        }
                        exportModel.Series.Add(newBar);
                    }
                }
                
                // Обновляем модель
                exportModel.InvalidatePlot(true);
                
                // Экспорт через OxyPlot.SkiaSharp (работает в фоновом потоке)
                using var stream = File.Create(filePath);
                var exporter = new PngExporter { Width = 800, Height = 600 };
                exporter.Export(exportModel, stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting chart: {ex.Message}");
            }
        });
    }
}
            

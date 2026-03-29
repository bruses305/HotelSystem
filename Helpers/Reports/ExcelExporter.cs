using ClosedXML.Excel;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using System.IO;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Класс для экспорта финансовых отчётов в Excel
/// </summary>
public class ExcelExporter
{
    private readonly IFinanceService _financeService;
    private readonly IRoomService _roomService;
    private readonly IClientService _clientService;
    private readonly IBookingService _bookingService;
    private readonly IServiceService _serviceService;
    
    public ExcelExporter(
        IFinanceService financeService,
        IRoomService roomService,
        IClientService clientService,
        IBookingService bookingService,
        IServiceService serviceService)
    {
        _financeService = financeService;
        _roomService = roomService;
        _clientService = clientService;
        _bookingService = bookingService;
        _serviceService = serviceService;
    }
    
    /// <summary>
    /// Экспортировать полный финансовый отчёт
    /// </summary>
    public async Task ExportAsync(DateTime startDate, DateTime endDate, string filePath)
    {
        var actualEndDate = endDate.Date.AddDays(1).AddSeconds(-1);
        
        // Получаем все данные
        var allBookings = (await _bookingService.GetBookingsByDateRangeAsync(startDate, actualEndDate)).ToList();
        var allClients = (await _clientService.GetAllClientsAsync()).ToList();
        var allRooms = (await _roomService.GetAllRoomsAsync()).ToList();
        var allServices = (await _serviceService.GetAllServicesAsync()).ToList();
        var transactions = await _financeService.GetTransactionsAsync(startDate, actualEndDate);
        var transactionsList = transactions.ToList();
        
        // Рассчитываем итоги
        var totalBookingPayments = allBookings.Sum(b => b.PaidAmount);
        var totalServiceIncome = transactionsList
            .Where(t => t.Type == TransactionType.Income && t.Category == TransactionCategory.AdditionalService)
            .Sum(t => t.Amount);
        var totalIncome = totalBookingPayments + totalServiceIncome;
        var totalExpenses = transactionsList.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var profit = totalIncome - totalExpenses;
        
        using var workbook = new XLWorkbook();
        
        // Создаём листы
        CreateFinanceSheet(workbook, startDate, actualEndDate, totalBookingPayments, totalServiceIncome, totalIncome, totalExpenses, profit);
        CreateAllOperationsSheet(workbook, allBookings, allClients, allRooms, allServices, transactionsList);
        CreateIncomeOnlySheet(workbook, allBookings, allClients, allRooms, allServices, transactionsList);
        CreateExpenseOnlySheet(workbook, allClients, allRooms, transactionsList);
        CreateIncomeByTypesSheet(workbook, allBookings, allServices, transactionsList, totalIncome);
        CreateExpenseByTypesSheet(workbook, transactionsList, totalExpenses);
        CreateIncomeByDaysSheet(workbook, allBookings, transactionsList, totalIncome);
        CreateIncomeByMonthsSheet(workbook, allBookings, transactionsList, totalIncome);
        CreateBookingsSheet(workbook, allBookings, allClients, allRooms);
        CreateRoomsSheet(workbook, allBookings, allRooms, transactionsList);
        CreateServicesSheet(workbook, allBookings, allClients, allServices, transactionsList);
        CreateServiceProfitSheet(workbook, allServices, transactionsList);
        CreateClientsSheet(workbook, allBookings, allClients, transactionsList);
        
        workbook.SaveAs(filePath);
    }
    
    private void CreateFinanceSheet(IXLWorkbook workbook, DateTime startDate, DateTime endDate,
        decimal bookingPayments, decimal serviceIncome, decimal totalIncome, decimal totalExpenses, decimal profit)
    {
        var sheet = workbook.Worksheets.Add("Финансы");
        
        sheet.Cell(1, 1).Value = "Финансовый отчёт";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 2).Merge();
        
        sheet.Cell(3, 1).Value = "Период:";
        sheet.Cell(3, 2).Value = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
        
        sheet.Cell(5, 1).Value = "Показатель";
        sheet.Cell(5, 2).Value = "Сумма";
        sheet.Range(5, 1, 5, 2).Style.Font.Bold = true;
        
        AddFinanceRow(sheet, 6, "Доходы от бронирований", bookingPayments);
        AddFinanceRow(sheet, 7, "Доходы от услуг", serviceIncome);
        AddFinanceRow(sheet, 8, "Общие доходы", totalIncome, true);
        
        sheet.Cell(10, 1).Value = "Общие расходы";
        sheet.Cell(10, 2).Value = (double)totalExpenses;
        sheet.Cell(10, 2).Style.NumberFormat.Format = "#,##0";
        
        sheet.Cell(11, 1).Value = "Прибыль";
        sheet.Cell(11, 1).Style.Font.Bold = true;
        sheet.Cell(11, 2).Value = (double)profit;
        sheet.Cell(11, 2).Style.Font.Bold = true;
        sheet.Cell(11, 2).Style.NumberFormat.Format = "#,##0";
        
        sheet.Columns().AdjustToContents();
    }
    
    private void AddFinanceRow(IXLWorksheet sheet, int row, string label, decimal value, bool bold = false)
    {
        sheet.Cell(row, 1).Value = label;
        if (bold) sheet.Cell(row, 1).Style.Font.Bold = true;
        
        sheet.Cell(row, 2).Value = (double)value;
        if (bold) sheet.Cell(row, 2).Style.Font.Bold = true;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
    }
    
    private void CreateAllOperationsSheet(IXLWorkbook workbook, List<Booking> bookings, 
        List<Client> clients, List<Room> rooms, List<Service> services, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Все операции");
        
        sheet.Cell(1, 1).Value = "Полный журнал операций";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 8).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Дата", "Тип", "Категория", "Номер", "Клиент", "Описание", "Сумма");
        
        int row = 4;
        bool alternate = false;
        
        // Оплаты бронирований
        foreach (var booking in bookings.Where(b => b.PaidAmount > 0))
        {
            var client = clients.FirstOrDefault(c => c.Id == booking.ClientId);
            var room = rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            
            ExcelStyles.AddDataRow(sheet, row, alternate,
                booking.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                "Доход", "Оплата бронирования",
                room?.Name ?? $"№{booking.RoomId}",
                client?.FullName ?? "Клиент",
                $"Бронирование #{booking.Id}",
                (double)booking.PaidAmount, true);
            
            row++;
            alternate = !alternate;
        }
        
        // Транзакции (без Booking - они уже учтены)
        foreach (var tx in transactions.Where(t => t.Category != TransactionCategory.Booking).OrderBy(t => t.TransactionDate))
        {
            var room = tx.RoomId.HasValue ? rooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
            var service = tx.ServiceId.HasValue ? services.FirstOrDefault(s => s.Id == tx.ServiceId) : null;
            string clientName = GetClientName(tx, bookings, clients);
            string category = CategoryHelper.GetDisplayCategory(tx, service);
            bool isIncome = tx.Type == TransactionType.Income;
            
            ExcelStyles.AddDataRow(sheet, row, alternate,
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
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 8).SetAutoFilter();
    }
    
    private void CreateIncomeOnlySheet(IXLWorkbook workbook, List<Booking> bookings,
        List<Client> clients, List<Room> rooms, List<Service> services, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Только доходы");
        
        sheet.Cell(1, 1).Value = "Все доходы";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 7).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Дата", "Категория", "Номер", "Клиент", "Описание", "Сумма");
        
        int row = 4;
        decimal total = 0;
        
        foreach (var booking in bookings.Where(b => b.PaidAmount > 0))
        {
            var client = clients.FirstOrDefault(c => c.Id == booking.ClientId);
            var room = rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            
            ExcelStyles.AddDataRowSimple(sheet, row, row % 2 == 0,
                booking.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                "Booking",
                room?.Name ?? $"№{booking.RoomId}",
                client?.FullName ?? "Клиент",
                $"Бронирование #{booking.Id}",
                (double)booking.PaidAmount);
            
            total += booking.PaidAmount;
            row++;
        }
        
        foreach (var tx in transactions.Where(t => t.Type == TransactionType.Income).OrderBy(t => t.TransactionDate))
        {
            var room = tx.RoomId.HasValue ? rooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
            var service = tx.ServiceId.HasValue ? services.FirstOrDefault(s => s.Id == tx.ServiceId) : null;
            string clientName = GetClientName(tx, bookings, clients);
            
            ExcelStyles.AddDataRowSimple(sheet, row, row % 2 == 0,
                tx.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                tx.Category.ToString(),
                service?.Name ?? room?.Name ?? "-",
                clientName,
                tx.Description ?? "-",
                (double)tx.Amount);
            
            total += tx.Amount;
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)total, 6);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 7).SetAutoFilter();
    }
    
    private void CreateExpenseOnlySheet(IXLWorkbook workbook, List<Client> clients, List<Room> rooms, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Только расходы");
        
        sheet.Cell(1, 1).Value = "Все расходы";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 7).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Дата", "Категория", "Номер", "Описание", "Количество", "Сумма");
        
        int row = 4;
        decimal total = 0;
        
        foreach (var tx in transactions.Where(t => t.Type == TransactionType.Expense).OrderBy(t => t.TransactionDate))
        {
            var room = tx.RoomId.HasValue ? rooms.FirstOrDefault(r => r.Id == tx.RoomId) : null;
            
            ExcelStyles.AddDataRowSimple(sheet, row, row % 2 == 0,
                tx.TransactionDate.ToString("dd.MM.yyyy HH:mm"),
                CategoryHelper.GetDisplayCategory(tx),
                room?.Name ?? (tx.RoomId.HasValue ? $"№{tx.RoomId}" : "-"),
                tx.Description ?? "-",
                tx.Quantity > 0 ? tx.Quantity.ToString() : "-",
                (double)tx.Amount);
            
            total += tx.Amount;
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)total, 6);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 7).SetAutoFilter();
    }
    
    private void CreateIncomeByTypesSheet(IXLWorkbook workbook, List<Booking> bookings, 
        List<Service> services, List<Transaction> transactions, decimal totalIncome)
    {
        var sheet = workbook.Worksheets.Add("Доходы по типам");
        
        sheet.Cell(1, 1).Value = "Доходы по типам";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 3).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Тип дохода", "Количество операций", "Сумма");
        
        int row = 4;
        
        // Оплаты бронирований
        var bookingPayments = bookings.Where(b => b.PaidAmount > 0).ToList();
        if (bookingPayments.Any())
        {
            sheet.Cell(row, 1).Value = "Оплата бронирований";
            sheet.Cell(row, 2).Value = bookingPayments.Count;
            sheet.Cell(row, 3).Value = (double)bookingPayments.Sum(b => b.PaidAmount);
            sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        
        // Доходы по услугам
        var incomeByService = transactions
            .Where(t => t.Type == TransactionType.Income && t.ServiceId.HasValue)
            .GroupBy(t => t.ServiceId)
            .Select(g => new {
                ServiceId = g.Key,
                ServiceName = services.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Услуга",
                Count = g.Sum(x => x.Quantity > 0 ? x.Quantity : 1),
                Sum = g.Sum(x => x.Amount)
            });
        
        foreach (var item in incomeByService)
        {
            sheet.Cell(row, 1).Value = $"Услуга: {item.ServiceName}";
            sheet.Cell(row, 2).Value = item.Count;
            sheet.Cell(row, 3).Value = (double)item.Sum;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)totalIncome, 3);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 3).SetAutoFilter();
    }
    
    private void CreateExpenseByTypesSheet(IXLWorkbook workbook, List<Transaction> transactions, decimal totalExpenses)
    {
        var sheet = workbook.Worksheets.Add("Расходы по типам");
        
        sheet.Cell(1, 1).Value = "Расходы по типам";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 3).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Тип расхода", "Количество операций", "Сумма");
        
        int row = 4;
        
        var expensesByDesc = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => CategoryHelper.GetExpenseCategoryKey(t))
            .Select(g => new { Category = g.Key, Count = g.Count(), Sum = g.Sum(t => t.Amount) });
        
        foreach (var item in expensesByDesc)
        {
            sheet.Cell(row, 1).Value = item.Category;
            sheet.Cell(row, 2).Value = item.Count;
            sheet.Cell(row, 3).Value = (double)item.Sum;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)totalExpenses, 3);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 3).SetAutoFilter();
    }
    
    private void CreateIncomeByDaysSheet(IXLWorkbook workbook, List<Booking> bookings, 
        List<Transaction> transactions, decimal totalIncome)
    {
        var sheet = workbook.Worksheets.Add("Доходы по дням");
        
        sheet.Cell(1, 1).Value = "Доходы по дням";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 2).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Дата", "Сумма");
        
        int row = 4;
        
        var bookingByDay = bookings
            .Where(b => b.PaidAmount > 0)
            .GroupBy(b => b.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(b => b.PaidAmount));
        
        var txByDay = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.TransactionDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        
        var allDates = bookingByDay.Keys.Union(txByDay.Keys).OrderBy(d => d);
        
        foreach (var date in allDates)
        {
            decimal dayTotal = 0;
            if (bookingByDay.ContainsKey(date)) dayTotal += bookingByDay[date];
            if (txByDay.ContainsKey(date)) dayTotal += txByDay[date];
            
            sheet.Cell(row, 1).Value = date.ToString("dd.MM.yyyy");
            sheet.Cell(row, 2).Value = (double)dayTotal;
            sheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)totalIncome, 2);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 2).SetAutoFilter();
    }
    
    private void CreateIncomeByMonthsSheet(IXLWorkbook workbook, List<Booking> bookings,
        List<Transaction> transactions, decimal totalIncome)
    {
        var sheet = workbook.Worksheets.Add("Доходы по месяцам");
        
        sheet.Cell(1, 1).Value = "Доходы по месяцам";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 2).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Месяц", "Сумма");
        
        int row = 4;
        
        var bookingByMonth = bookings
            .Where(b => b.PaidAmount > 0)
            .GroupBy(b => b.CreatedAt.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(b => b.PaidAmount));
        
        var txByMonth = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.TransactionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        
        var allMonths = bookingByMonth.Keys.Union(txByMonth.Keys).OrderBy(m => m);
        
        foreach (var month in allMonths)
        {
            decimal monthTotal = 0;
            if (bookingByMonth.ContainsKey(month)) monthTotal += bookingByMonth[month];
            if (txByMonth.ContainsKey(month)) monthTotal += txByMonth[month];
            
            var parts = month.Split('-');
            var monthName = new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), 1).ToString("MMMM yyyy");
            
            sheet.Cell(row, 1).Value = monthName;
            sheet.Cell(row, 2).Value = (double)monthTotal;
            sheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)totalIncome, 2);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 2).SetAutoFilter();
    }
    
    private void CreateBookingsSheet(IXLWorkbook workbook, List<Booking> bookings, List<Client> clients, List<Room> rooms)
    {
        var sheet = workbook.Worksheets.Add("Бронирования");
        
        sheet.Cell(1, 1).Value = "Список бронирований";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 9).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "ID", "Номер", "Клиент", "Заезд", "Выезд", "Статус", "Оплачено", "Сумма", "Дней");
        
        int row = 4;
        foreach (var booking in bookings)
        {
            var client = clients.FirstOrDefault(c => c.Id == booking.ClientId);
            var room = rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            
            sheet.Cell(row, 1).Value = booking.Id;
            sheet.Cell(row, 2).Value = room?.Name ?? $"№{booking.RoomId}";
            sheet.Cell(row, 3).Value = client?.FullName ?? "Клиент";
            sheet.Cell(row, 4).Value = booking.CheckInDate.ToString("dd.MM.yyyy");
            sheet.Cell(row, 5).Value = booking.CheckOutDate.ToString("dd.MM.yyyy");
            sheet.Cell(row, 6).Value = booking.Status.ToString();
            sheet.Cell(row, 7).Value = (double)booking.PaidAmount;
            sheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(row, 8).Value = (double)booking.TotalPrice;
            sheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            sheet.Cell(row, 9).Value = booking.Days;
            row++;
        }
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 9).SetAutoFilter();
    }
    
    private void CreateRoomsSheet(IXLWorkbook workbook, List<Booking> bookings, List<Room> rooms, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Номера");
        
        sheet.Cell(1, 1).Value = "Прибыльность номеров";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 6).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Номер", "Тип", "Доход от бронирований", "Доход от услуг", "Расходы", "Прибыль");
        
        int row = 4;
        foreach (var room in rooms)
        {
            var roomBookings = bookings.Where(b => b.RoomId == room.Id);
            var incomeBookings = roomBookings.Sum(b => b.PaidAmount);
            
            var roomTx = transactions.Where(t => t.RoomId == room.Id && t.Type == TransactionType.Income);
            var incomeServices = roomTx.Sum(t => t.Amount);
            
            var expenses = transactions
                .Where(t => t.RoomId == room.Id && t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);
            
            var profit = incomeBookings + incomeServices - expenses;
            
            sheet.Cell(row, 1).Value = room.Name;
            sheet.Cell(row, 2).Value = room.Type.ToString();
            AddNumberCell(sheet, row, 3, (double)incomeBookings);
            AddNumberCell(sheet, row, 4, (double)incomeServices);
            AddNumberCell(sheet, row, 5, (double)expenses);
            AddNumberCell(sheet, row, 6, (double)profit);
            row++;
        }
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 6).SetAutoFilter();
    }
    
    private void CreateServicesSheet(IXLWorkbook workbook, List<Booking> bookings,
        List<Client> clients, List<Service> services, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Услуги");
        
        sheet.Cell(1, 1).Value = "Операции по услугам";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 7).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Дата", "Услуга", "Клиент", "Бронирование", "Количество", "Сумма");
        
        int row = 4;
        decimal total = 0;
        
        var serviceTxs = transactions.Where(t => t.ServiceId.HasValue).OrderBy(t => t.TransactionDate);
        
        foreach (var tx in serviceTxs)
        {
            var service = services.FirstOrDefault(s => s.Id == tx.ServiceId);
            string clientName = GetClientName(tx, bookings, clients);
            
            sheet.Cell(row, 1).Value = tx.TransactionDate.ToString("dd.MM.yyyy HH:mm");
            sheet.Cell(row, 2).Value = service?.Name ?? "Услуга";
            sheet.Cell(row, 3).Value = clientName;
            sheet.Cell(row, 4).Value = tx.BookingId.HasValue ? $"#{tx.BookingId}" : "-";
            sheet.Cell(row, 5).Value = tx.Quantity > 0 ? tx.Quantity : 1;
            AddNumberCell(sheet, row, 6, (double)tx.Amount);
            
            total += tx.Amount;
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)total, 6);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 7).SetAutoFilter();
    }
    
    private void CreateServiceProfitSheet(IXLWorkbook workbook, List<Service> services, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Прибыль по услугам");
        
        sheet.Cell(1, 1).Value = "Прибыль по услугам";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 4).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Услуга", "Количество продаж", "Выручка", "Цена за единицу");
        
        int row = 4;
        decimal total = 0;
        
        var stats = transactions
            .Where(t => t.ServiceId.HasValue && t.Type == TransactionType.Income)
            .GroupBy(t => t.ServiceId)
            .Select(g => new {
                ServiceId = g.Key,
                ServiceName = services.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Услуга",
                Count = g.Sum(t => t.Quantity > 0 ? t.Quantity : 1),
                Revenue = g.Sum(t => t.Amount),
                Price = g.First().Amount / (g.First().Quantity > 0 ? g.First().Quantity : 1)
            })
            .OrderByDescending(s => s.Revenue);
        
        foreach (var stat in stats)
        {
            sheet.Cell(row, 1).Value = stat.ServiceName;
            sheet.Cell(row, 2).Value = stat.Count;
            AddNumberCell(sheet, row, 3, (double)stat.Revenue);
            AddNumberCell(sheet, row, 4, (double)stat.Price);
            
            total += stat.Revenue;
            row++;
        }
        
        ExcelStyles.AddTotalRow(sheet, row, "ИТОГО:", (double)total, 3);
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 4).SetAutoFilter();
    }
    
    private void CreateClientsSheet(IXLWorkbook workbook, List<Booking> bookings, List<Client> clients, List<Transaction> transactions)
    {
        var sheet = workbook.Worksheets.Add("Клиенты");
        
        sheet.Cell(1, 1).Value = "Клиенты";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Range(1, 1, 1, 6).Merge();
        
        ExcelStyles.AddHeaderRow(sheet, 3, "Клиент", "Бронирований", "Потрачено на номера", "Услуг куплено", "Потрачено на услуги", "Всего потрачено");
        
        int row = 4;
        
        foreach (var client in clients)
        {
            var clientBookings = bookings.Where(b => b.ClientId == client.Id);
            var spentOnRooms = clientBookings.Sum(b => b.PaidAmount);
            
            var clientServiceTxs = transactions
                .Where(t => t.BookingId.HasValue && bookings.Any(b => b.Id == t.BookingId && b.ClientId == client.Id))
                .ToList();
            
            var servicesBought = clientServiceTxs.Sum(t => t.Quantity > 0 ? t.Quantity : 1);
            var spentOnServices = clientServiceTxs.Sum(t => t.Amount);
            var totalSpent = spentOnRooms + spentOnServices;
            var bookingCount = clientBookings.Count();
            
            if (bookingCount > 0 || servicesBought > 0)
            {
                sheet.Cell(row, 1).Value = client.FullName;
                sheet.Cell(row, 2).Value = bookingCount;
                AddNumberCell(sheet, row, 3, (double)spentOnRooms);
                sheet.Cell(row, 4).Value = servicesBought;
                AddNumberCell(sheet, row, 5, (double)spentOnServices);
                var totalCell = sheet.Cell(row, 6);
                totalCell.Value = (double)totalSpent;
                totalCell.Style.NumberFormat.Format = "#,##0";
                totalCell.Style.Font.Bold = true;
                row++;
            }
        }
        
        sheet.Columns().AdjustToContents();
        sheet.Range(3, 1, row - 1, 6).SetAutoFilter();
    }
    
    private void AddNumberCell(IXLWorksheet sheet, int row, int col, double value)
    {
        var cell = sheet.Cell(row, col);
        cell.Value = value;
        cell.Style.NumberFormat.Format = "#,##0";
    }
    
    private string GetClientName(Transaction tx, List<Booking> bookings, List<Client> clients)
    {
        if (!tx.BookingId.HasValue) return "";
        
        var booking = bookings.FirstOrDefault(b => b.Id == tx.BookingId);
        if (booking == null) return "";
        
        var client = clients.FirstOrDefault(c => c.Id == booking.ClientId);
        return client?.FullName ?? "";
    }
}

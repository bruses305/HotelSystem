using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class FinanceService : IFinanceService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ILogService _logService;

    public FinanceService(
        ITransactionRepository transactionRepository,
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        IClientRepository clientRepository,
        IServiceRepository serviceRepository,
        ILogService logService)
    {
        _transactionRepository = transactionRepository;
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _clientRepository = clientRepository;
        _serviceRepository = serviceRepository;
        _logService = logService;
    }

 public async Task<decimal> GetTotalIncomeAsync(DateTime? startDate = null, DateTime? endDate = null)
 {
 return await _transactionRepository.GetTotalIncomeAsync(startDate, endDate);
 }

 public async Task<decimal> GetTotalExpensesAsync(DateTime? startDate = null, DateTime? endDate = null)
 {
 return await _transactionRepository.GetTotalExpensesAsync(startDate, endDate);
 }

 public async Task<decimal> GetProfitAsync(DateTime? startDate = null, DateTime? endDate = null)
 {
 var income = await GetTotalIncomeAsync(startDate, endDate);
 var expenses = await GetTotalExpensesAsync(startDate, endDate);
 return income - expenses;
 }

    public async Task<FinanceReport> GetFinanceReportAsync(DateTime startDate, DateTime endDate)
    {
        var report = new FinanceReport();
        
        // Получаем транзакции
        var transactions = (await _transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate)).ToList();
        
        // Сохраняем транзакции для использования в отчётах
        report.Transactions = transactions;
        
        // Основные показатели из транзакций
        report.TotalIncome = (double)transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        report.TotalExpenses = (double)transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        report.Profit = report.TotalIncome - report.TotalExpenses;

        // Группировка по категориям доходов
        report.IncomeByCategory = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => (double)g.Sum(t => t.Amount));

        // Группировка по категориям расходов
        report.ExpensesByCategory = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => (double)g.Sum(t => t.Amount));

        // Доходы по номерам
        report.IncomeByRoom = transactions
            .Where(t => t.Type == TransactionType.Income && t.RoomId.HasValue)
            .GroupBy(t => t.RoomId!.Value)
            .ToDictionary(g => g.Key, g => (double)g.Sum(t => t.Amount));

        // Доходы по месяцам
        report.IncomeByMonth = transactions
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.TransactionDate.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => (double)g.Sum(t => t.Amount));

        // Детализация бронирований - получаем все бронирования за период
        var bookings = await _bookingRepository.GetBookingsByDateRangeAsync(startDate, endDate);
        var clients = await _clientRepository.GetAllAsync();
        var rooms = await _roomRepository.GetAllAsync();
        
        foreach (var booking in bookings)
        {
            var client = clients.FirstOrDefault(c => c.Id == booking.ClientId);
            var room = rooms.FirstOrDefault(r => r.Id == booking.RoomId);
            
            // Добавляем информацию об оплате бронирования
            if (booking.PaidAmount > 0)
            {
                report.BookingDetails.Add(new BookingTransactionDetail
                {
                    Date = booking.CreatedAt,
                    RoomName = room?.Name ?? $"№{booking.RoomId}",
                    ClientName = client?.FullName ?? "Клиент",
                    Amount = booking.PaidAmount,
                    Type = "Оплата бронирования"
                });
            }
            
            // Добавляем информацию о расходах (коммунальные, уборка)
            var bookingTransactions = transactions.Where(t => t.BookingId == booking.Id && t.Type == TransactionType.Expense);
            foreach (var tx in bookingTransactions)
            {
                report.BookingDetails.Add(new BookingTransactionDetail
                {
                    Date = tx.TransactionDate,
                    RoomName = room?.Name ?? $"№{booking.RoomId}",
                    ClientName = client?.FullName ?? "Клиент",
                    Amount = tx.Amount,
                    Type = tx.Description
                });
                report.TotalExpenses += (double)tx.Amount;
            }
        }
        
        // Пересчитываем прибыль
        report.Profit = report.TotalIncome - report.TotalExpenses;

        // Детализация услуг
        var services = await _serviceRepository.GetAllAsync();
        var serviceTxs = transactions.Where(t => t.ServiceId.HasValue && t.Type == TransactionType.Income);
        foreach (var tx in serviceTxs)
        {
            var service = services.FirstOrDefault(s => s.Id == tx.ServiceId);
            var booking = bookings.FirstOrDefault(b => b.Id == tx.BookingId);
            var client = clients.FirstOrDefault(c => c.Id == booking?.ClientId);
            
            report.ServiceDetails.Add(new ServiceTransactionDetail
            {
                Date = tx.TransactionDate,
                ServiceName = service?.Name ?? "Услуга",
                ClientName = client?.FullName ?? "Клиент",
                Quantity = tx.Quantity,
                Amount = tx.Amount
            });
        }
        
        return report;
    }

 public async Task<Transaction> AddTransactionAsync(Transaction transaction)
 {
 var created = await _transactionRepository.AddAsync(transaction);
 await _logService.LogAsync(LogLevel.Medium, 
 $"Добавлена транзакция: {transaction.Type} {transaction.Amount} пользователем: {AuthService.CurrentEmployee.FullName}", "FinanceService");
 return created;
 }

 public async Task<IEnumerable<Transaction>> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null)
 {
 if (startDate.HasValue && endDate.HasValue)
 {
 return await _transactionRepository.GetTransactionsByDateRangeAsync(startDate.Value, endDate.Value);
 }
 return await _transactionRepository.GetAllWithDetailsAsync();
 }

    public async Task RecordBookingPaymentAsync(int bookingId, decimal amount)
    {
        // Обновляем статус бронирования и TotalSpent клиента
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking != null)
        {
            var room = await _roomRepository.GetByIdAsync(booking.RoomId);
            var client = await _clientRepository.GetByIdAsync(booking.ClientId);
            
            booking.PaidAmount += amount;
            if (booking.PaidAmount >= booking.TotalPrice)
            {
                booking.Status = BookingStatus.Paid;
            }
            await _bookingRepository.UpdateAsync(booking);
            
            // Обновляем TotalSpent клиента
            if (client != null)
            {
                client.TotalSpent += amount;
                await _clientRepository.UpdateAsync(client);
            }
            
            // Создаём транзакцию с подробным описанием (включая номер комнаты)
            var transaction = new Transaction
            {
                Type = TransactionType.Income,
                Category = TransactionCategory.Booking,
                Amount = amount,
                BookingId = bookingId,
                RoomId = booking.RoomId,
                TransactionDate = DateTime.Now,
                Description = $"Оплата бронирования #{bookingId} (Номер: {room?.Name ?? "№" + booking.RoomId}) - {client?.FullName ?? "Клиент"}"
            };

            await AddTransactionAsync(transaction);
        }
    }

    public async Task RecordServicePaymentAsync(int bookingId, int serviceId, int quantity, decimal amount)
    {
        var booking = await _bookingRepository.GetBookingWithDetailsAsync(bookingId);
        var service = await _serviceRepository.GetByIdAsync(serviceId);
        
        // Создаём транзакцию с подробным описанием
        var transaction = new Transaction
        {
            Type = TransactionType.Income,
            Category = TransactionCategory.AdditionalService,
            Amount = amount,
            BookingId = bookingId,
            RoomId = booking?.RoomId,
            ServiceId = serviceId,
            Quantity = quantity,
            TransactionDate = DateTime.Now,
            Description = $"Услуга \"{service?.Name ?? "Услуга"}\" для бронирования #{bookingId} (Номер: {booking?.Room?.Name ?? "№" + booking?.RoomId})"
        };

        await AddTransactionAsync(transaction);
        
        // Обновляем TotalSpent клиента
        if (booking != null)
        {
            var client = await _clientRepository.GetByIdAsync(booking.ClientId);
            if (client != null)
            {
                client.TotalSpent += amount;
                await _clientRepository.UpdateAsync(client);
            }
        }
        
        // Обновляем счётчик покупок услуги
        if (service != null)
        {
            service.PurchaseCount += quantity;
            await _serviceRepository.UpdateAsync(service);
        }
    }

 public async Task RecordSalaryPaymentAsync(int employeeId, decimal amount)
 {
 var transaction = new Transaction
 {
 Type = TransactionType.Expense,
 Category = TransactionCategory.Salary,
 Amount = amount,
 EmployeeId = employeeId,
 TransactionDate = DateTime.Now,
 Description = $"Выплата зарплаты сотруднику #{employeeId}"
 };

 await AddTransactionAsync(transaction);
 }

 public async Task RecordRoomExpenseAsync(int roomId, string category, decimal amount)
 {
 var transaction = new Transaction
 {
 Type = TransactionType.Expense,
 Category = TransactionCategory.Maintenance,
 Amount = amount,
 RoomId = roomId,
 TransactionDate = DateTime.Now,
 Description = $"Расход по номеру #{roomId}: {category}"
 };

 await AddTransactionAsync(transaction);
 }
}

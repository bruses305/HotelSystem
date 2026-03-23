using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class FinanceService : IFinanceService
{
 private readonly ITransactionRepository _transactionRepository;
 private readonly IBookingRepository _bookingRepository;
 private readonly IRoomRepository _roomRepository;
 private readonly ILogService _logService;

 public FinanceService(
 ITransactionRepository transactionRepository,
 IBookingRepository bookingRepository,
 IRoomRepository roomRepository,
 ILogService logService)
 {
 _transactionRepository = transactionRepository;
 _bookingRepository = bookingRepository;
 _roomRepository = roomRepository;
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
 var transactions = (await _transactionRepository.GetTransactionsByDateRangeAsync(startDate, endDate)).ToList();
        
 var report = new FinanceReport
 {
 TotalIncome = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => (double)t.Amount),
 TotalExpenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => (double)t.Amount),
 Profit =0
 };

 report.Profit = report.TotalIncome - report.TotalExpenses;

 // Группировка по категориям доходов
 report.IncomeByCategory = transactions
 .Where(t => t.Type == TransactionType.Income)
 .GroupBy(t => t.Category)
 .ToDictionary(g => g.Key, g => g.Sum(t => (double)t.Amount));

 // Группировка по категориям расходов
 report.ExpensesByCategory = transactions
 .Where(t => t.Type == TransactionType.Expense)
 .GroupBy(t => t.Category)
 .ToDictionary(g => g.Key, g => g.Sum(t => (double)t.Amount));

 // Доходы по номерам
 report.IncomeByRoom = transactions
 .Where(t => t.Type == TransactionType.Income && t.RoomId.HasValue)
 .GroupBy(t => t.RoomId!.Value)
 .ToDictionary(g => g.Key, g => g.Sum(t => (double)t.Amount));

 // Доходы по месяцам
 report.IncomeByMonth = transactions
 .Where(t => t.Type == TransactionType.Income)
 .GroupBy(t => t.TransactionDate.ToString("yyyy-MM"))
 .ToDictionary(g => g.Key, g => g.Sum(t => (double)t.Amount));

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
 var transaction = new Transaction
 {
 Type = TransactionType.Income,
 Category = TransactionCategory.Booking,
 Amount = amount,
 BookingId = bookingId,
 TransactionDate = DateTime.Now,
 Description = $"Оплата бронирования #{bookingId}"
 };

 await AddTransactionAsync(transaction);

 // Обновляем статус бронирования
 var booking = await _bookingRepository.GetByIdAsync(bookingId);
 if (booking != null)
 {
 booking.PaidAmount += amount;
 if (booking.PaidAmount >= booking.TotalPrice)
 {
 booking.Status = BookingStatus.Paid;
 }
 await _bookingRepository.UpdateAsync(booking);
 }
 }

 public async Task RecordServicePaymentAsync(int bookingId, int serviceId, int quantity, decimal amount)
 {
 var booking = await _bookingRepository.GetBookingWithDetailsAsync(bookingId);
        
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
 Description = $"Оплата дополнительной услуги для бронирования #{bookingId}"
 };

 await AddTransactionAsync(transaction);
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

using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

/// <summary>
/// Сервис для расчёта себестоимости номеров
/// Формула: (Общие расходы отеля / Общая площадь) × Площадь номера / Загрузка номера
/// </summary>
public class RoomCostCalculationService : IRoomCostCalculationService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IEmployeeService _employeeService;
    private readonly IRoomRepository _roomRepository;

    public RoomCostCalculationService(
        IExpenseRepository expenseRepository,
        IEmployeeService employeeService,
        IRoomRepository roomRepository)
    {
        _expenseRepository = expenseRepository;
        _employeeService = employeeService;
        _roomRepository = roomRepository;
    }

    /// <summary>
    /// Расчитывает себестоимость номера в сутки
    /// </summary>
    /// <param name="room">Номер для расчёта</param>
    /// <param name="occupancyRate">Коэффициент загрузки (0.0 - 1.0), по умолчанию 0.8</param>
    /// <returns>Себестоимость в сутки</returns>
    public async Task<decimal> CalculateRoomCostAsync(Room room, decimal occupancyRate = 0.8m)
    {
        // 1. Получаем общие ежемесячные расходы
        var expenses = await _expenseRepository.GetAllAsync();
        var monthlyExpenses = expenses.Sum(e => e.Amount);

        // 2. Получаем зарплату сотрудников
        var employees = await _employeeService.GetAllEmployeesAsync();
        var totalSalary = employees.Where(e => e.IsActive).Sum(e => e.Salary);

        // 3. Общие расходы отеля в месяц
        var totalMonthlyExpenses = monthlyExpenses + totalSalary;

        // 4. Получаем все номера и считаем общую площадь
        var allRooms = await _roomRepository.GetAllAsync();
        var totalArea = allRooms.Sum(r => r.Area);

        if (totalArea == 0)
            return 0;

        // 5. Расходы на 1 м² в месяц
        var costPerSqm = totalMonthlyExpenses / totalArea;

        // 6. Расходы на конкретный номер в месяц
        var roomMonthlyCost = costPerSqm * room.Area;

        // 7. Считаем количество занятых дней в месяц
        var occupiedDaysPerMonth = 30 * occupancyRate;

        if (occupiedDaysPerMonth == 0)
            occupiedDaysPerMonth = 1; // Защита от деления на ноль

        // 8. Себестоимость в сутки
        var dailyCost = roomMonthlyCost / occupiedDaysPerMonth;

        return Math.Round(dailyCost, 2);
    }

    /// <summary>
    /// Расчитывает себестоимость для всех номеров
    /// </summary>
    public async Task<Dictionary<int, decimal>> CalculateAllRoomsCostAsync()
    {
        var result = new Dictionary<int, decimal>();
        var rooms = await _roomRepository.GetAllAsync();

        foreach (var room in rooms)
        {
            result[room.Id] = await CalculateRoomCostAsync(room);
        }

        return result;
    }

    /// <summary>
    /// Получает детальную информацию о расчёте
    /// </summary>
    public async Task<CostCalculationDetails> GetCalculationDetailsAsync(Room room, decimal occupancyRate = 0.8m)
    {
        var expenses = await _expenseRepository.GetAllAsync();
        var monthlyExpenses = expenses.Sum(e => e.Amount);

        var employees = await _employeeService.GetAllEmployeesAsync();
        var totalSalary = employees.Where(e => e.IsActive).Sum(e => e.Salary);

        var totalMonthlyExpenses = monthlyExpenses + totalSalary;

        var allRooms = await _roomRepository.GetAllAsync();
        var totalArea = allRooms.Sum(r => r.Area);

        var costPerSqm = totalArea > 0 ? totalMonthlyExpenses / totalArea : 0;
        var roomMonthlyCost = costPerSqm * room.Area;
        var occupiedDaysPerMonth = 30 * occupancyRate;
        var dailyCost = occupiedDaysPerMonth > 0 ? roomMonthlyCost / occupiedDaysPerMonth : 0;

        return new CostCalculationDetails
        {
            TotalMonthlyExpenses = totalMonthlyExpenses,
            MonthlyExpensesFromDB = monthlyExpenses,
            TotalSalary = totalSalary,
            TotalArea = totalArea,
            CostPerSqm = costPerSqm,
            RoomArea = room.Area,
            RoomMonthlyCost = roomMonthlyCost,
            OccupiedDaysPerMonth = occupiedDaysPerMonth,
            DailyCost = Math.Round(dailyCost, 2)
        };
    }
}

/// <summary>
/// Детали расчёта себестоимости
/// </summary>
public class CostCalculationDetails
{
    public decimal TotalMonthlyExpenses { get; set; } // Все расходы отеля в месяц
    public decimal MonthlyExpensesFromDB { get; set; } // Доп расходы из БД
    public decimal TotalSalary { get; set; } // ЗП сотрудников
    public decimal TotalArea { get; set; } // Общая площадь всех номеров
    public decimal CostPerSqm { get; set; } // Расходы на 1 м²
    public decimal RoomArea { get; set; } // Площадь номера
    public decimal RoomMonthlyCost { get; set; } // Расходы на номер в месяц
    public decimal OccupiedDaysPerMonth { get; set; } // Занятые дни в месяц
    public decimal DailyCost { get; set; } // Себестоимость в сутки
}
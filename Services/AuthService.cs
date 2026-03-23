using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public class AuthService : IAuthService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogService _logService;

    private static Employee? _currentEmployee;
    public static Employee? CurrentEmployee => _currentEmployee;
    public bool IsAuthenticated => _currentEmployee != null;
    public bool IsAdmin => _currentEmployee?.Role == UserRole.Admin;

    public event EventHandler<Employee?>? AuthChanged;

    public AuthService(IEmployeeRepository employeeRepository, ILogService logService)
    {
        _employeeRepository = employeeRepository;
        _logService = logService;
    }

    public async Task<Employee?> LoginAsync(string login, string password)
    {
        var isValid = await _employeeRepository.ValidatePasswordAsync(login, password);
        if (!isValid)
        {
            await _logService.LogAsync(LogLevel.Critical, 
                $"Попытка входа в аккаунт: {login}", "AuthService");
            return null;
        }

        _currentEmployee = await _employeeRepository.GetByLoginAsync(login);
        
        await _logService.LogAsync(LogLevel.Medium, 
            $"Пользователь {_currentEmployee?.FullName} вошол в аккаунт", "AuthService");
        
        AuthChanged?.Invoke(this, _currentEmployee);
        
        return _currentEmployee;
    }

    public async Task LogoutAsync()
    {
        if (_currentEmployee != null)
        {
            await _logService.LogAsync(LogLevel.Low, 
                $"Пользователь {_currentEmployee.FullName} вышел из аккаунта", "AuthService");
        }
        
        _currentEmployee = null;
        AuthChanged?.Invoke(this, null);
    }
}

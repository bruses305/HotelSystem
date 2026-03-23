using Microsoft.Extensions.DependencyInjection;
using HotelSystem.Data;
using HotelSystem.Repositories;
using HotelSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem.Helpers;

public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider ServiceProvider => _serviceProvider 
        ?? throw new InvalidOperationException("ServiceProvider not initialized");

    public static void Initialize()
    {
        var services = new ServiceCollection();
        var connectionString = "Data Source=hotel.db";
        services.AddDbContext<HotelDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INavigationService, NavigationService>();
        _serviceProvider = services.BuildServiceProvider();
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
        context.Database.EnsureCreated();
    }

    public static T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}

using System.Windows;
using System.Windows.Threading;
using HotelSystem.Data;
using HotelSystem.Helpers;
using HotelSystem.Views;
using Microsoft.EntityFrameworkCore;

namespace HotelSystem;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Устанавливаем русскую культуру для всего приложения
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
        
        base.OnStartup(e);
        
        // Глобальный обработчик исключений
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        ServiceLocator.Initialize();
        
        // Заполнение базы данных тестовыми данными
        try
        {
            var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hotel.db");
            var connectionString = $"Data Source={dbPath}";
            try
            {
                using var context = new HotelDbContext(connectionString);
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка EnsureCreated: {ex.Message}\n{ex.InnerException?.Message}");
                throw;
            }
            // Заполняем данными
            //await SeedData.SeedAsync(context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка при заполнении БД: {ex.Message}");
            MessageBox.Show($"Ошибка при заполнении БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        var loginWindow = new LoginWindow();
        loginWindow.Show();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"РћС€РёР±РєР°: {e.Exception.Message}", "РћС€РёР±РєР° РїСЂРёР»РѕР¶РµРЅРёСЏ", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"РљСЂРёС‚РёС‡РµСЃРєР°СЏ РѕС€РёР±РєР°: {ex.Message}", "РћС€РёР±РєР°", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}


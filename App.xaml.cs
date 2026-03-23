using System.Windows;
using System.Windows.Threading;
using HotelSystem.Helpers;
using HotelSystem.Views;

namespace HotelSystem;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Р“Р»РѕР±Р°Р»СЊРЅС‹Р№ РѕР±СЂР°Р±РѕС‚С‡РёРє РёСЃРєР»СЋС‡РµРЅРёР№
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        ServiceLocator.Initialize();
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


using System.Windows;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService? _authService;

    public LoginWindow()
    {
        InitializeComponent();
        try
        {
            _authService = ServiceLocator.GetService<IAuthService>();
            LoginTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        
        if (_authService == null)
        {
            ShowError("Сервис авторизации недоступен");
            return;
        }
        
        var login = LoginTextBox.Text.Trim();
        var password = PasswordBox.Password;
        
        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)) 
        { 
            ShowError("Введите логин и пароль"); 
            return; 
        }
        
        try
        {
            var employee = await _authService.LoginAsync(login, password);
            
            if (employee == null)
            {
                ShowError("Неверный логин или пароль");
                PasswordBox.Clear();
                return;
            }
            
            // Проверка на null
            if (employee.Id == 0 || string.IsNullOrEmpty(employee.FullName))
            {
                ShowError("Ошибка данных сотрудника");
                PasswordBox.Clear();
                return;
            }
            
            // Создаём главное окно
            try
            {
                var mainWindow = new MainWindow(employee);
                
                // Устанавливаем главное окно приложения
                Application.Current.MainWindow = mainWindow;
                
                mainWindow.Show();
                Close(); // Закрываем login window
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка открытия главного окна: {ex.Message}");
                PasswordBox.Clear();
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка входа: {ex.Message}");
            PasswordBox.Clear();
        }
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }
}



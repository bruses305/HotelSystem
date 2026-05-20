using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HotelSystem.Services;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;
using MaterialDesignThemes.Wpf;

namespace HotelSystem.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService? _authService;

        public LoginWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в конструкторе: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            try
            {
                _authService = ServiceLocator.GetService<IAuthService>();
                LoginTextBox.Focus();
                LoadRememberMe();

                TogglePasswordVisibility.Checked += (s, e) => ShowPassword();
                TogglePasswordVisibility.Unchecked += (s, e) => HidePassword();

                VisiblePasswordTextBox.TextChanged += (s, e) =>
                {
                    if (VisiblePasswordTextBox.Visibility == Visibility.Visible)
                        PasswordBox.Password = VisiblePasswordTextBox.Text;
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var enterAnimation = TryFindResource("WindowEnterAnimation") as Storyboard;
            enterAnimation?.Begin(this);
        }

        // Исправленный метод перетаскивания (без 'is not')
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBox || Equals(e.OriginalSource, PasswordBox) || e.OriginalSource is CheckBox)
                return;
            DragMove();
        }

        private void ShowPassword()
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            VisiblePasswordTextBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Focus();
        }

        private void HidePassword()
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
        }

        private void ShakeCard()
        {
            if (LoginCard.RenderTransform == null || !(LoginCard.RenderTransform is TranslateTransform))
                LoginCard.RenderTransform = new TranslateTransform();

            var shake = TryFindResource("ShakeAnimation") as Storyboard;
            if (shake != null)
            {
                Storyboard.SetTarget(shake, LoginCard);
                shake.Begin();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginButton.IsEnabled = false;
            LoginProgressBar.Visibility = Visibility.Visible;
            ErrorBorder.Visibility = Visibility.Collapsed;

            if (_authService == null)
            {
                ShowError("Сервис авторизации недоступен");
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            string login = LoginTextBox.Text.Trim();
            string password = (VisiblePasswordTextBox.Visibility == Visibility.Visible)
                ? VisiblePasswordTextBox.Text
                : PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите логин и пароль");
                ShakeCard();
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                var employee = await _authService.LoginAsync(login, password);

                if (employee == null)
                {
                    ShowError("Неверный логин или пароль");
                    ShakeCard();
                    ClearPassword();
                    return;
                }

                if (employee.Id == 0 || string.IsNullOrEmpty(employee.FullName))
                {
                    ShowError("Ошибка данных сотрудника");
                    ShakeCard();
                    return;
                }

                SaveRememberMe(login);

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
                fadeOut.Completed += (s, _) =>
                {
                    var mainWindow = new MainWindow(employee);
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    Close();
                };
                BeginAnimation(OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка входа: {ex.Message}");
                ShakeCard();
                ClearPassword();
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearPassword()
        {
            if (VisiblePasswordTextBox.Visibility == Visibility.Visible)
                VisiblePasswordTextBox.Clear();
            else
                PasswordBox.Clear();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Функция восстановления пароля будет доступна в следующей версии.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ========== Сохранение "Запомнить меня" через JSON ==========
        private class UserSettings
        {
            public bool RememberLogin { get; set; }
            public string LastLogin { get; set; } = "";
        }

        private static string SettingsPath => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HotelSystem", "user_settings.json");

        private void LoadRememberMe()
        {
            try
            {
                if (System.IO.File.Exists(SettingsPath))
                {
                    string json = System.IO.File.ReadAllText(SettingsPath);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<UserSettings>(json);
                    if (settings != null && settings.RememberLogin)
                    {
                        LoginTextBox.Text = settings.LastLogin;
                        RememberMeCheckBox.IsChecked = true;
                    }
                }
            }
            catch { }
        }

        private void SaveRememberMe(string login)
        {
            try
            {
                var settings = new UserSettings
                {
                    RememberLogin = RememberMeCheckBox.IsChecked == true,
                    LastLogin = RememberMeCheckBox.IsChecked == true ? login : ""
                };
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                string dir = System.IO.Path.GetDirectoryName(SettingsPath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
        
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Находим подсказку внутри шаблона PasswordBox
            var hint = FindVisualChild<TextBlock>(PasswordBox, "hint");
            if (hint != null)
            {
                hint.Visibility = string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

// Вспомогательный метод для поиска элемента по имени
        private T FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && t.Name == name) return t;
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
        
        
    }
    public class TextToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
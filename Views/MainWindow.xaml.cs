using System.Windows;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class MainWindow : Window
{
    private Employee _currentUser;

    public MainWindow(Employee user)
    {
        try
        {
            InitializeComponent();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "Employee object is null");
            }

            _currentUser = user;
            
            if (UserRoleText != null)
            {
                UserRoleText.Text = user.Role == UserRole.Admin ? "Администратор" : "Работник";
            }

            if (user.Role != UserRole.Admin)
            {
                // Для не-админов скрываем пункты меню на основе прав
                if (EmployeesMenuItem != null && !PermissionChecker.CanView(PermissionCategory.Employees))
                    EmployeesMenuItem.Visibility = Visibility.Collapsed;
                if (LogsMenuItem != null && !PermissionChecker.CanView(PermissionCategory.Logs))
                    LogsMenuItem.Visibility = Visibility.Collapsed;
                if (ReportsMenuItem != null && !PermissionChecker.CanView(PermissionCategory.Reports))
                    ReportsMenuItem.Visibility = Visibility.Collapsed;
                if (SettingsMenuItem != null && !PermissionChecker.CanView(PermissionCategory.Settings))
                    SettingsMenuItem.Visibility = Visibility.Collapsed;
                if (RolesMenuItem != null && !PermissionChecker.CanView(PermissionCategory.RoleManagement))
                    RolesMenuItem.Visibility = Visibility.Collapsed;
            }

            NavigateToBookings();

            // Подписываемся на изменения уведомлений
            NotificationService.Instance.NotificationsChanged += () =>
                Dispatcher.Invoke(() => UpdateNotificationBadge());

            // Генерируем уведомления о бронированиях и обновляем кружок
            _ = NotificationService.Instance.GenerateBookingNotificationsAsync();
            UpdateNotificationBadge();

            // Запускаем таймер для периодической проверки уведомлений
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5);
            timer.Tick += async (_, _) =>
            {
                await NotificationService.Instance.GenerateBookingNotificationsAsync();
                UpdateNotificationBadge();
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка инициализации главного окна: {ex.Message}\n\n{ex.StackTrace}", 
                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void UpdateNotificationBadge()
    {
        var unreadCount = NotificationService.Instance.UnreadCount;
        NotificationBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NavigateToBookings(object? sender = null, RoutedEventArgs? e = null)
    {
        MainFrame.Navigate(new BookingsView());
        PageTitle.Text = "Бронирования";
    }

    private void NavigateToRooms(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new RoomsView());
        PageTitle.Text = "Номера";
    }

    private void NavigateToClients(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new ClientsView());
        PageTitle.Text = "Клиенты";
    }

    private void NavigateToCalendar(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new CalendarView());
        PageTitle.Text = "Календарь";
    }

    private void NavigateToFinance(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new FinanceView());
        PageTitle.Text = "Финансы";
    }

    private void NavigateToServices(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new ServicesView());
        PageTitle.Text = "Услуги";
    }

    private void NavigateToEmployees(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new EmployeesView());
        PageTitle.Text = "Сотрудники";
    }

    private void NavigateToReports(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new ReportsView());
        PageTitle.Text = "Отчёты";
    }

    private void NavigateToLogs(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new LogsView());
        PageTitle.Text = "Логи";
    }

    private void NavigateToSettings(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new SettingsView());
        PageTitle.Text = "Настройки";
    }

    private void NavigateToServicesPayment(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new ServicesPaymentView());
        PageTitle.Text = "Оплата услуг";
    }

    private void ShowNotifications_Click(object sender, RoutedEventArgs e)
    {
        var notificationsWindow = new NotificationsWindow();
        notificationsWindow.Owner = this;
        if (notificationsWindow.ShowDialog() == true && notificationsWindow.SelectedBookingId.HasValue)
        {
            // Переходим на вкладку бронирований с выделением
            MainFrame.Navigate(new BookingsView(notificationsWindow.SelectedBookingId));
        }
        
        // Обновляем кружок после закрытия окна
        UpdateNotificationBadge();
    }

    private void ShowProfile_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Профиль: {_currentUser.FullName}", "Профиль");
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Выйти из системы?", "Подтверждение", MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            var loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();
            Close();
        }
    }

    private void ManageRoles_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanView(PermissionCategory.RoleManagement))
        {
            MessageBox.Show("Недостаточно прав для управления ролями!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new RoleManagerWindow();
        dialog.Owner = this;
        dialog.ShowDialog();
    }
}

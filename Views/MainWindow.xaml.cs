using System.Windows;
using System.Windows.Controls;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class MainWindow : Window
{
    private Employee _currentUser = null!;

    public MainWindow(Employee user)
    {
        InitializeComponent();

        _currentUser = user;
        UserRoleText.Text = user.Role == UserRole.Admin ? "Администратор" : "Работник";

        if (user.Role != UserRole.Admin)
        {
            EmployeesMenuItem.Visibility = Visibility.Collapsed;
            LogsMenuItem.Visibility = Visibility.Collapsed;
            ReportsMenuItem.Visibility = Visibility.Collapsed;
            SettingsMenuItem.Visibility = Visibility.Collapsed;
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
        timer.Tick += async (s, e) =>
        {
            await NotificationService.Instance.GenerateBookingNotificationsAsync();
            UpdateNotificationBadge();
        };
        timer.Start();
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
}

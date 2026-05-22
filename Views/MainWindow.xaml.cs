using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
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
                if (ExpensesMenuItem != null && !PermissionChecker.CanView(PermissionCategory.Expenses))
                    ExpensesMenuItem.Visibility = Visibility.Collapsed;
            }

            // Подписываемся на событие навигации для анимации
            MainFrame.Navigated += MainFrame_Navigated;

            NavigateToBookings();

            NotificationService.Instance.NotificationsChanged += () =>
                Dispatcher.Invoke(() => UpdateNotificationBadge());

            _ = NotificationService.Instance.GenerateBookingNotificationsAsync();
            UpdateNotificationBadge();

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

    private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        if (e.Content is FrameworkElement content)
        {
            var anim = TryFindResource("FrameEnterAnimation") as Storyboard;
            if (anim != null)
            {
                content.Opacity = 0;
                anim.Begin(content);
            }
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
    public void NavigateToRooms(int? roomId = null)
    {
        var roomsView = new RoomsView(roomId);
        MainFrame.Navigate(roomsView);
        PageTitle.Text = "Номера";
    }

    public void NavigateToClients(int? clientId = null)
    {
        var clientsView = new ClientsView(clientId);
        MainFrame.Navigate(clientsView);
        PageTitle.Text = "Клиенты";
    }

    private void NavigateToCalendar(object sender, RoutedEventArgs e)
    {
        MainFrame.Navigate(new CalendarView());
        PageTitle.Text = "Календарь";
    }

    private void NavigateToFinance(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanView(PermissionCategory.Finance))
        {
            MessageBox.Show("Недостаточно прав для просмотра финансов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
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

    private void NavigateToExpenses(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanView(PermissionCategory.Expenses))
        {
            MessageBox.Show("Недостаточно прав для просмотра расходов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MainFrame.Navigate(new ExpensesView());
        PageTitle.Text = "Дополнительные расходы";
    }

    private void ShowNotifications_Click(object sender, RoutedEventArgs e)
    {
        var notificationsWindow = new NotificationsWindow();
        notificationsWindow.Owner = this;
        if (notificationsWindow.ShowDialog() == true && notificationsWindow.SelectedBookingId.HasValue)
        {
            MainFrame.Navigate(new BookingsView(notificationsWindow.SelectedBookingId));
        }
        UpdateNotificationBadge();
    }

    private void ShowProfile_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Профиль: {_currentUser.FullName}", "Профиль");
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Выйти из системы?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Models;

namespace HotelSystem.Views;

public partial class NotificationsWindow : Window
{
    public int? SelectedBookingId { get; private set; }

    public NotificationsWindow()
    {
        InitializeComponent();
        NotificationsList.ItemsSource = NotificationService.Instance.Notifications;
    }

    private void GoToBooking_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem notif && notif.BookingId > 0)
        {
            // Переход к бронированию
            var notification =
                NotificationService.Instance.Notifications.FirstOrDefault(n => n.BookingId == notif.BookingId);
            if (notification != null)
                _ = NotificationService.Instance.MarkAsRead(notification.Id);

            SelectedBookingId = notif.BookingId;
            DialogResult = true;
            Close();
        }
    }

    private void MarkRead_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem notif)
        {
            _ = NotificationService.Instance.MarkAsRead(notif.Id);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is NotificationItem notif)
        {
            _ = NotificationService.Instance.RemoveNotification(notif.Id);
        }
    }


    private void MarkAllRead_Click(object sender, RoutedEventArgs e)
    {
        _ = NotificationService.Instance.MarkAllAsRead();
    }

    private void DeleteAll_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Удалить все уведомления?", "Подтверждение", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _ = NotificationService.Instance.ClearAll();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

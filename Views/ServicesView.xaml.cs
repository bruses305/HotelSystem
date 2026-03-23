using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ServicesView : Page
{
    private readonly IServiceService _serviceService;
    public ServicesView()
    {
        InitializeComponent();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        LoadServicesAsync();
    }
    private async void LoadServicesAsync()
    {
        try { ServicesGrid.ItemsSource = await _serviceService.GetAllServicesAsync(); }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private async void AddService_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ServiceDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try { await _serviceService.CreateServiceAsync(dialog.Service); LoadServicesAsync(); MessageBox.Show("Услуга добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
    private void EditService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Service service)
        {
            var dialog = new ServiceDialog(service);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) { _ = _serviceService.UpdateServiceAsync(dialog.Service); LoadServicesAsync(); }
        }
    }
    private async void DeleteService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Service service)
        {
            var result = MessageBox.Show($"Удалить услугу {service.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) { await _serviceService.DeleteServiceAsync(service.Id); LoadServicesAsync(); }
        }
    }
}



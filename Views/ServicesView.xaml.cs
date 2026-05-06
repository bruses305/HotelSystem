using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ServicesView : Page
{
    private readonly IServiceService _serviceService;
    private List<Service> _allServices = new();
    
    public ServicesView()
    {
        InitializeComponent();
        _serviceService = ServiceLocator.GetService<IServiceService>();
        LoadServicesAsync();
        CheckPermissions();
    }
    
    private async void LoadServicesAsync()
    {
        try 
        {
            _allServices = (List<Service>)await _serviceService.GetAllServicesAsync();
            ApplyFilter();
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ApplyFilter()
    {
        var searchQuery = SearchTextBox.Text;
        var filtered = SearchHelper.FilterServices(_allServices, searchQuery);
        ServicesGrid.ItemsSource = filtered.ToList();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyFilter();
        }
    }

    private void ResetFilter_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        ApplyFilter();
    }
    
    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Services))
        {
            if (FindName("AddServiceButton") is Button addButton)
            {
                addButton.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    private void AddService_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Services))
        {
            MessageBox.Show("Недостаточно прав для создания услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new ServiceDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try { _ = _serviceService.CreateServiceAsync(dialog.Service); LoadServicesAsync(); MessageBox.Show("Услуга добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
    
    private void EditService_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Services))
        {
            MessageBox.Show("Недостаточно прав для редактирования услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Service service)
        {
            var dialog = new ServiceDialog(service);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true) { _ = _serviceService.UpdateServiceAsync(dialog.Service); LoadServicesAsync(); }
        }
    }
    
    private async void DeleteService_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanDelete(PermissionCategory.Services))
        {
            MessageBox.Show("Недостаточно прав для удаления услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Service service)
        {
            var result = MessageBox.Show($"Удалить услугу {service.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) { await _serviceService.DeleteServiceAsync(service.Id); LoadServicesAsync(); }
        }
    }

    private void ServicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Services))
        {
            MessageBox.Show("Недостаточно прав для редактирования услуг!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (ServicesGrid.SelectedItem is Service service)
        {
            var dialog = new ServiceDialog(service);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _serviceService.UpdateServiceAsync(dialog.Service);
                LoadServicesAsync();
            }
        }
    }
}

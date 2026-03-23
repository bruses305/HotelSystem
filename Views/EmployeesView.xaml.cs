using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class EmployeesView : Page
{
    private readonly IEmployeeService _employeeService;

    public EmployeesView()
    {
        InitializeComponent();
        _employeeService = ServiceLocator.GetService<IEmployeeService>();
        LoadEmployeesAsync();
    }

    private async void LoadEmployeesAsync()
    {
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            EmployeesGrid.ItemsSource = employees;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°: {ex.Message}", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddEmployee_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EmployeeDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _employeeService.CreateEmployeeAsync(dialog.Employee);
                LoadEmployeesAsync();
                MessageBox.Show("Р В Р Р‹Р В РЎвЂўР РЋРІР‚С™Р РЋР вЂљР РЋРЎвЂњР В РўвЂР В Р вЂ¦Р В РЎвЂР В РЎвЂќ Р В РўвЂР В РЎвЂўР В Р’В±Р В Р’В°Р В Р вЂ Р В Р’В»Р В Р’ВµР В Р вЂ¦!", "Р В Р в‚¬Р РЋР С“Р В РЎвЂ”Р В Р’ВµР РЋРІР‚В¦", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°: {ex.Message}", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Employee employee)
        {
            var dialog = new EmployeeDialog(employee);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _employeeService.UpdateEmployeeAsync(dialog.Employee);
                LoadEmployeesAsync();
            }
        }
    }

    private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Employee employee)
        {
            if (employee.Login == "admin")
            {
                MessageBox.Show("Р В РЎСљР В Р’ВµР В Р’В»Р РЋР Р‰Р В Р’В·Р РЋР РЏ Р РЋРЎвЂњР В РўвЂР В Р’В°Р В Р’В»Р В РЎвЂР РЋРІР‚С™Р РЋР Р‰ Р В Р’В°Р В РўвЂР В РЎВР В РЎвЂР В Р вЂ¦Р В РЎвЂР РЋР С“Р РЋРІР‚С™Р РЋР вЂљР В Р’В°Р РЋРІР‚С™Р В РЎвЂўР РЋР вЂљР В Р’В°!", "Р В РЎвЂєР РЋРІвЂљВ¬Р В РЎвЂР В Р’В±Р В РЎвЂќР В Р’В°", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show($"Р В Р в‚¬Р В РўвЂР В Р’В°Р В Р’В»Р В РЎвЂР РЋРІР‚С™Р РЋР Р‰ Р РЋР С“Р В РЎвЂўР РЋРІР‚С™Р РЋР вЂљР РЋРЎвЂњР В РўвЂР В Р вЂ¦Р В РЎвЂР В РЎвЂќР В Р’В° {employee.FullName}?", "Р В РЎСџР В РЎвЂўР В РўвЂР РЋРІР‚С™Р В Р вЂ Р В Р’ВµР РЋР вЂљР В Р’В¶Р В РўвЂР В Р’ВµР В Р вЂ¦Р В РЎвЂР В Р’Вµ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _employeeService.DeleteEmployeeAsync(employee.Id);
                LoadEmployeesAsync();
            }
        }
    }
}


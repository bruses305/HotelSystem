using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class EmployeesView : Page
{
    private readonly IEmployeeService _employeeService;
    private readonly RoleService _roleService;
    private List<Role> _roles = new();

    public EmployeesView()
    {
        InitializeComponent();
        _employeeService = ServiceLocator.GetService<IEmployeeService>();
        _roleService = ServiceLocator.GetService<RoleService>();
        _ = LoadRolesAsync();
        LoadEmployeesAsync();
        CheckPermissions();
    }

    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Employees) && FindName("AddEmployeeButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadRolesAsync()
    {
        _roles = (await _roleService.GetAllRolesAsync()).ToList();
        // Добавляем администратора в список
        var adminRole = new Role { Id = 0, Name = "Администратор" };
        _roles.Insert(0, adminRole);
        EmployeesGrid.DataContext = this;
    }

public List<Role> Roles => _roles;

    private async void LoadEmployeesAsync()
    {
        try
        {
            var employees = (await _employeeService.GetActiveEmployeesAsync()).ToList();
            // Загружаем роли для всех сотрудников
            foreach (var emp in employees)
            {
                if (emp.RoleId.HasValue)
                {
                    emp.RoleEntity = await _roleService.GetRoleByIdAsync(emp.RoleId.Value);
                }
            }
            EmployeesGrid.ItemsSource = employees;
        }
        catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async void AddEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Employees))
        {
            MessageBox.Show("Недостаточно прав для создания сотрудников!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new EmployeeDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _employeeService.CreateEmployeeAsync(dialog.Employee);
                LoadEmployeesAsync();
                MessageBox.Show("Сотрудник добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
            
    private void EditEmployee_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Employees))
        {
            MessageBox.Show("Недостаточно прав для редактирования сотрудников!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
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
        if (!PermissionChecker.CanDelete(PermissionCategory.Employees))
        {
            MessageBox.Show("Недостаточно прав для удаления сотрудников!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Employee employee)
        {
            if (employee.Login == "admin")
            {
                MessageBox.Show("Администратор не может быть удалён!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var result = MessageBox.Show($"Удалить сотрудника {employee.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await _employeeService.DeleteEmployeeAsync(employee.Id);
                LoadEmployeesAsync();
            }
        }
    }

    private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Employees))
        {
            MessageBox.Show("Недостаточно прав для редактирования сотрудников!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (EmployeesGrid.SelectedItem is Employee employee)
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
}

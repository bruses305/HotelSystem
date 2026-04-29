using System.Windows;
using System.Windows.Controls;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class EmployeeDialog : Window
{
    public Employee Employee { get; private set; }
    private readonly bool _isEdit;
    private bool _isSaved = false;
    private readonly RoleService _roleService;
    
    // Сохраняем оригинальные значения
    private string _originalFullName = "";
    private string _originalLogin = "";
    private string _originalPosition = "";
    private string _originalPhone = "";

    public EmployeeDialog(Employee? employee = null)
    {
        InitializeComponent();
        _roleService = ServiceLocator.GetService<RoleService>();
        _isEdit = employee != null;
        Employee = employee ?? new Employee();
        _ = LoadRolesAsync();
        if (_isEdit) InitializeForm();
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _roleService.GetAllRolesAsync();
        RoleComboBox.ItemsSource = roles;
        
        // Добавляем администратора как отдельную опцию
        var adminRole = new Role { Id = 0, Name = "Администратор (полный доступ)" };
        var rolesList = roles.ToList();
        rolesList.Insert(0, adminRole);
        RoleComboBox.ItemsSource = rolesList;
    }

    private void InitializeForm()
    {
        FullNameTextBox.Text = Employee.FullName;
        LoginTextBox.Text = Employee.Login;
        PositionTextBox.Text = Employee.Position;
        PhoneTextBox.Text = Employee.Phone;
        SalaryTextBox.Text = Employee.Salary.ToString();

        // Выбираем роль
        if (Employee.Role == UserRole.Admin)
        {
            RoleComboBox.SelectedIndex = 0; // Администратор
        }
        else if (Employee.RoleId.HasValue)
        {
            var rolesList = (RoleComboBox.ItemsSource as List<Role>)!;
            for (int i = 1; i < rolesList.Count; i++) // Начинаем с 1, пропускаем админа
            {
                if (rolesList[i].Id == Employee.RoleId)
                {
                    RoleComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        _originalFullName = Employee.FullName ?? "";
        _originalLogin = Employee.Login ?? "";
        _originalPosition = Employee.Position ?? "";
        _originalPhone = Employee.Phone ?? "";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
        {
            MessageBox.Show("Введите ФИО", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
        {
            MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var password = PasswordBox.Password;
        if (string.IsNullOrEmpty(password) && !_isEdit)
        {
            MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(password))
        {
            Employee.PasswordHash = password;
        }
        else if (_isEdit && !string.IsNullOrEmpty(Employee.PasswordHash))
        {
            // Не меняем пароль
        }

        Employee.FullName = FullNameTextBox.Text;
        Employee.Login = LoginTextBox.Text;
        Employee.Position = PositionTextBox.Text;
        Employee.Phone = PhoneTextBox.Text;
        Employee.Salary = decimal.TryParse(SalaryTextBox.Text, out var s) ? s : 0;
        Employee.IsActive = true;

        // Сохраняем роль
        var selectedRole = RoleComboBox.SelectedItem as Role;
        if (selectedRole != null && selectedRole.Id == 0)
        {
            Employee.Role = UserRole.Admin;
            Employee.RoleId = null;
        }
        else if (selectedRole != null)
        {
            Employee.Role = UserRole.Custom;
            Employee.RoleId = selectedRole.Id;
        }
        else
        {
            Employee.Role = UserRole.Custom;
            Employee.RoleId = null;
        }

        _isSaved = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isSaved) return;

        var currentFullName = FullNameTextBox.Text ?? "";
        var currentLogin = LoginTextBox.Text ?? "";
        var currentPosition = PositionTextBox.Text ?? "";
        var currentPhone = PhoneTextBox.Text ?? "";
        var currentPassword = PasswordBox.Password;

        bool hasChanges = _isEdit
            ? currentFullName != _originalFullName || currentLogin != _originalLogin ||
              currentPosition != _originalPosition || currentPhone != _originalPhone ||
              !string.IsNullOrEmpty(currentPassword)
            : !string.IsNullOrEmpty(currentFullName) || !string.IsNullOrEmpty(currentLogin) ||
              !string.IsNullOrEmpty(currentPosition) || !string.IsNullOrEmpty(currentPhone) ||
              !string.IsNullOrEmpty(currentPassword);

        if (hasChanges)
        {
            var result = MessageBox.Show("Есть несохранённые изменения. Закрыть?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}

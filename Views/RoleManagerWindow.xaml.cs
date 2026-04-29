using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class RoleManagerWindow : Window
{
    private readonly RoleService _roleService;
    private readonly PermissionService _permissionService;
    private Role? _selectedRole;
    private List<PermissionViewModel> _allPermissions = new();
    private string _currentColor = "#3498DB";

    public RoleManagerWindow()
    {
        InitializeComponent();
        _roleService = ServiceLocator.GetService<RoleService>();
        _permissionService = ServiceLocator.GetService<PermissionService>();
        CheckPermissions();
        _ = LoadDataAsync();
    }

    private void CheckPermissions()
    {
        var canCreate = PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Create);
        var canEdit = PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Edit);
        var canDelete = PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Delete);
        
        if (!canCreate && FindName("AddRoleButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
        
        if (!canDelete && FindName("DeleteRoleButton") is Button deleteButton)
        {
            deleteButton.Visibility = Visibility.Collapsed;
        }
        
        if (!canEdit)
        {
            if (FindName("SaveRoleButton") is Button saveButton)
            {
                saveButton.Visibility = Visibility.Collapsed;
            }
            if (FindName("SavePermissionsButton") is Button savePermButton)
            {
                savePermButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async Task LoadDataAsync()
    {
        var roles = await _roleService.GetAllRolesAsync();
        RolesListBox.ItemsSource = roles;
        
        var permissions = await _permissionService.GetAllPermissionsAsync();
        _allPermissions = permissions.Select(p => new PermissionViewModel { Permission = p }).ToList();
    }

    private void RolesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RolesListBox.SelectedItem is Role role)
        {
            _selectedRole = role;
            RoleNameTextBox.Text = role.Name;
            RoleDescriptionTextBox.Text = role.Description;
            
            _currentColor = role.BackgroundColor;
            UpdateColorButton(_currentColor);
            
            _ = LoadRolePermissionsAsync();
        }
    }
        
    private async Task LoadRolePermissionsAsync()
    {
        if (_selectedRole == null) return;
        
        var rolePermissions = await _permissionService.GetRolePermissionsAsync(_selectedRole.Id);
        var rolePermissionIds = rolePermissions.Select(p => p.Id).ToHashSet();
        
        foreach (var vm in _allPermissions)
        {
            vm.IsSelected = rolePermissionIds.Contains(vm.Permission.Id);
        }
        
        PermissionsItemsControl.ItemsSource = _allPermissions;
    }
    
    private void UpdateColorButton(string hexColor)
    {
        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            RoleColorButton.Background = brush;
            RoleColorButton.Content = hexColor.ToUpper();
            
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            var textBrush = brightness > 0.5 ? Brushes.Black : Brushes.White;
            RoleColorButton.Foreground = textBrush;
        }
        catch
        {
            RoleColorButton.Background = Brushes.Gray;
            RoleColorButton.Content = "#808080";
            RoleColorButton.Foreground = Brushes.White;
        }
    }

    private void RoleColorButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ColorPickerDialog(_currentColor);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                _currentColor = dialog.SelectedColor;
                UpdateColorButton(_currentColor);
                
                if (_selectedRole != null)
                {
                    _selectedRole.BackgroundColor = _currentColor;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при открытии выбора цвета:\n{ex.Message}\n\n{ex.StackTrace}", 
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddRole_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Create))
        {
            MessageBox.Show("Недостаточно прав для создания ролей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var name = Microsoft.VisualBasic.Interaction.InputBox("Введите название роли:", "Новая роль", "Новая роль");
        if (string.IsNullOrWhiteSpace(name)) return;
        
        var role = await _roleService.CreateRoleAsync(name, "");
        RolesListBox.SelectedItem = role;
        await LoadDataAsync();
    }

    private async void DeleteRole_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Delete))
        {
            MessageBox.Show("Недостаточно прав для удаления ролей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (_selectedRole == null || _selectedRole.IsSystem)
        {
            MessageBox.Show("Нельзя удалить системную роль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = MessageBox.Show($"Удалить роль {_selectedRole.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        await _roleService.DeleteRoleAsync(_selectedRole.Id);
        _selectedRole = null;
        RoleNameTextBox.Text = "";
        RoleDescriptionTextBox.Text = "";
        await LoadDataAsync();
    }

    private async void SaveRole_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Edit))
        {
            MessageBox.Show("Недостаточно прав для редактирования ролей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (_selectedRole == null) return;
        
        _selectedRole.BackgroundColor = _currentColor;
        _selectedRole.TextColor = "#FFFFFF";
        
        await _roleService.UpdateRoleAsync(_selectedRole.Id, RoleNameTextBox.Text, RoleDescriptionTextBox.Text, _currentColor, "#FFFFFF");
        
        MessageBox.Show("Роль сохранена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        await LoadDataAsync();
    }

    private async void SavePermissions_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.HasPermission(PermissionCategory.RoleManagement, PermissionType.Edit))
        {
            MessageBox.Show("Недостаточно прав для редактирования прав ролей!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (_selectedRole == null) return;
        
        var selectedIds = _allPermissions.Where(p => p.IsSelected).Select(p => p.Permission.Id).ToList();
        await _permissionService.SetRolePermissionsAsync(_selectedRole.Id, selectedIds);
        MessageBox.Show("Права сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class PermissionViewModel
{
    public Permission Permission { get; set; } = null!;
    public bool IsSelected { get; set; }
    
    public string Category => Permission.Category.ToString();
    public string Type => Permission.Type.ToString();
}

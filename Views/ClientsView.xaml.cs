using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Views;

public partial class ClientsView : Page
{
    private readonly IClientService _clientService;
    private int? _highlightClientId;
    private List<Client> _allClients = new();

    public ClientsView() : this(null) { }

    public ClientsView(int? highlightClientId = null)
    {
        InitializeComponent();
        _clientService = ServiceLocator.GetService<IClientService>();
        _highlightClientId = highlightClientId;
        LoadClientsAsync();
        CheckPermissions();
    }
    
    private void CheckPermissions()
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Clients) && FindName("AddClientButton") is Button addButton)
        {
            addButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void LoadClientsAsync()
    {
        try
        {
            _allClients = (await _clientService.GetAllClientsAsync()).ToList();
            ClientsGrid.ItemsSource = _allClients;

            // Выделение нужного клиента
            if (_highlightClientId.HasValue)
            {
                var index = _allClients.FindIndex(c => c.Id == _highlightClientId.Value);
                if (index >= 0)
                {
                    ClientsGrid.SelectedIndex = index;
                    ClientsGrid.ScrollIntoView(ClientsGrid.Items[index]);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyClientSearch()
    {
        try
        {
            var searchTerm = SearchTextBox.Text?.Trim();
            _ = Task.Run(async () =>
            {
                var allClients = await _clientService.GetAllClientsAsync();
                
                var filtered = string.IsNullOrWhiteSpace(searchTerm)
                    ? allClients
                    : SearchHelper.FilterClients(allClients, searchTerm);
                
                var sorted = string.IsNullOrWhiteSpace(searchTerm)
                    ? filtered
                    : SearchHelper.SortClientsByPriority(filtered, searchTerm);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ClientsGrid.ItemsSource = sorted.ToList();
                });
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void AddClient_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanCreate(PermissionCategory.Clients))
        {
            MessageBox.Show("Недостаточно прав для создания клиентов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new ClientDialog();
        dialog.Owner = Window.GetWindow(this);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _clientService.CreateClientAsync(dialog.Client);
                LoadClientsAsync();
                MessageBox.Show("Клиент добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditClient_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Clients))
        {
            MessageBox.Show("Недостаточно прав для редактирования клиентов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Client client)
        {
            var dialog = new ClientDialog(client);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _clientService.UpdateClientAsync(dialog.Client);
                LoadClientsAsync();
            }
        }
    }

    private void ClientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!PermissionChecker.CanEdit(PermissionCategory.Clients))
        {
            MessageBox.Show("Недостаточно прав для редактирования клиентов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (ClientsGrid.SelectedItem is Client client)
        {
            var dialog = new ClientDialog(client);
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                _ = _clientService.UpdateClientAsync(dialog.Client);
                LoadClientsAsync();
            }
        }
    }

    private async void DeleteClient_Click(object sender, RoutedEventArgs e)
    {
        if (!PermissionChecker.CanDelete(PermissionCategory.Clients))
        {
            MessageBox.Show("Недостаточно прав для удаления клиентов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (sender is Button btn && btn.Tag is Client client)
        {
            var result = MessageBox.Show($"Удалить клиента {client.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _clientService.DeleteClientAsync(client.Id);
                    LoadClientsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        ApplyClientSearch();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyClientSearch();
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyClientSearch();
        }
    }
}
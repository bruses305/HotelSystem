using System.Windows;
using HotelSystem.Views;
using System.Windows.Controls;
using System.Windows.Input;
using HotelSystem.Repositories;
using HotelSystem.Services;
using HotelSystem.Models.Entities;
using HotelSystem.Helpers;

namespace HotelSystem.Views;

public partial class ClientsView : Page
{
 private readonly IClientService _clientService;

 public ClientsView()
 {
 InitializeComponent();
 _clientService = ServiceLocator.GetService<IClientService>();
 LoadClientsAsync();
 }

 private async void LoadClientsAsync()
 {
 try
 {
 var clients = await _clientService.GetAllClientsAsync();
 ClientsGrid.ItemsSource = clients;
 }
 catch (Exception ex)
 {
 MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private async void AddClient_Click(object sender, RoutedEventArgs e)
 {
 var dialog = new ClientDialog();
 dialog.Owner = Window.GetWindow(this);
 if (dialog.ShowDialog() == true)
 {
 try
 {
 await _clientService.CreateClientAsync(dialog.Client);
 LoadClientsAsync();
 MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
 }
 catch (Exception ex)
 {
 MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }
 }

 private void EditClient_Click(object sender, RoutedEventArgs e)
 {
 if (sender is Button btn && btn.Tag is Client client)
 {
 var dialog = new ClientDialog(client);
 dialog.Owner = Window.GetWindow(this);
 if (dialog.ShowDialog() == true)
 {
 _ = UpdateClientAsync(dialog.Client);
 }
 }
 }

 private async Task UpdateClientAsync(Client client)
 {
 try
 {
 await _clientService.UpdateClientAsync(client);
 LoadClientsAsync();
 }
 catch (Exception ex)
 {
 MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private async void DeleteClient_Click(object sender, RoutedEventArgs e)
 {
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

 private async void Search_Click(object sender, RoutedEventArgs e)
 {
 try
 {
 var searchTerm = SearchTextBox.Text.Trim();
 if (string.IsNullOrEmpty(searchTerm))
 {
 LoadClientsAsync();
 return;
 }

 var clients = await _clientService.SearchClientsAsync(searchTerm);
 ClientsGrid.ItemsSource = clients;
 }
 catch (Exception ex)
 {
 MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
 }
 }

 private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
 {
 if (string.IsNullOrEmpty(SearchTextBox.Text?.Trim()))
 {
 LoadClientsAsync();
 }
 }

 private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
 {
 if (e.Key == Key.Enter)
 {
 Search_Click(sender, e);
 }
 }

 private void ClientsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
 {
 if (ClientsGrid.SelectedItem is Client client)
 {
 var dialog = new ClientDialog(client);
 dialog.Owner = Window.GetWindow(this);
 dialog.ShowDialog();
 }
 }
}

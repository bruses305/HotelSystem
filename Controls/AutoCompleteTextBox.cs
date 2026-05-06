using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Controls;

/// <summary>
/// TextBox с автодополнением и выбором из списка
/// </summary>
public class AutoCompleteTextBox : TextBox
{
    private Popup _popup;
    private ListBox _listBox;
    private List<Client> _allClients = new();
    private List<Client> _filteredClients = new();
    private Client? _selectedClient;
    private readonly IClientService _clientService;
    private Func<Client?, Task>? _onClientSelected;
    private Func<Task<Client?>?>? _onCreateClient;

    public AutoCompleteTextBox()
    {
        _clientService = ServiceLocator.GetService<IClientService>();
        InitializePopup();
        Loaded += OnLoaded;
        TextChanged += OnTextChanged;
        PreviewKeyDown += OnKeyDown;
        LostFocus += OnLostFocus;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = LoadClientsAsync();
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            _allClients = (await _clientService.GetAllClientsAsync()).ToList();
        }
        catch { }
    }

    private void InitializePopup()
    {
        _popup = new Popup
        {
            PlacementTarget = this,
            Placement = PlacementMode.Bottom,
            AllowsTransparency = true,
            StaysOpen = false,
            VerticalOffset = 2
        };

        _listBox = new ListBox
        {
            MaxHeight = 200,
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            ItemContainerStyle = new Style(typeof(ListBoxItem))
            {
                Setters = { new Setter(FrameworkElement.CursorProperty, Cursors.Hand) }
            }
        };

        _listBox.SelectionChanged += ListBox_SelectionChanged;
        _listBox.MouseDoubleClick += ListBox_MouseDoubleClick;
        _popup.Child = _listBox;
    }

    private async void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = Text.Trim().ToLower();
        
        if (string.IsNullOrEmpty(query))
        {
            _popup.IsOpen = false;
            return;
        }

        _filteredClients = _allClients
            .Where(c => c.FullName.ToLower().Contains(query) || 
                       c.Phone != null && c.Phone.Contains(query) ||
                       c.Id.ToString().Contains(query))
            .Take(10)
            .ToList();

        if (_filteredClients.Any())
        {
            _listBox.ItemsSource = _filteredClients;
            _listBox.DisplayMemberPath = "FullName";
            _listBox.SelectedIndex = 0;
            _popup.IsOpen = true;
        }
        else
        {
            _popup.IsOpen = false;
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_popup.IsOpen) return;

        switch (e.Key)
        {
            case Key.Down:
                if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                    _listBox.SelectedIndex++;
                e.Handled = true;
                break;
                
            case Key.Up:
                if (_listBox.SelectedIndex > 0)
                    _listBox.SelectedIndex--;
                e.Handled = true;
                break;
                
            case Key.Enter:
                if (_listBox.SelectedItem is Client client)
                {
                    SelectClient(client);
                    _popup.IsOpen = false;
                }
                else if (_filteredClients.Count == 0 && _onCreateClient != null)
                {
                    _popup.IsOpen = false;
                    _ = CreateNewClient();
                }
                e.Handled = true;
                break;
                
            case Key.Escape:
                _popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_listBox.SelectedItem is Client client)
        {
            _selectedClient = client;
            Text = client.FullName;
            CaretIndex = Text.Length;
        }
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_listBox.SelectedItem is Client client)
        {
            SelectClient(client);
            _popup.IsOpen = false;
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        _popup.IsOpen = false;
    }

    private void SelectClient(Client client)
    {
        _selectedClient = client;
        Text = client.FullName;
        _ = _onClientSelected?.Invoke(client);
    }

    private async Task CreateNewClient()
    {
        var newClient = await _onCreateClient?.Invoke();
        if (newClient != null)
        {
            _allClients.Add(newClient);
            SelectClient(newClient);
        }
    }

    public void SetClientSelectedHandler(Func<Client?, Task> handler)
    {
        _onClientSelected = handler;
    }

    public void SetCreateClientHandler(Func<Task<Client?>?> handler)
    {
        _onCreateClient = handler;
    }

    public void ClearSelection()
    {
        _selectedClient = null;
        Text = "";
        _popup.IsOpen = false;
    }

    public Client? SelectedClient => _selectedClient;

    public void SetClients(List<Client> clients)
    {
        _allClients = clients;
    }

    public void RefreshClients()
    {
        _ = LoadClientsAsync();
    }
}

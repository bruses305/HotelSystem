using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using HotelSystem.Models.Entities;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Controls;

public class ClientAutoCompleteBox : UserControl
{
    private TextBox _inputBox;
    private Popup _popup;
    private ListBox _listBox;
    private List<Client> _allClients = new();
    private List<Client> _filteredClients = new();
    private Client? _selectedClient;
    private readonly IClientService _clientService;
    private Func<Client?, Task>? _onClientSelected;
    private Func<Task<Client?>?>? _onCreateClient;
    private bool _hasMatches = false;
    private bool _isProgrammaticUpdate = false;   // <-- добавить

    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(ClientAutoCompleteBox), 
            new PropertyMetadata("Введите имя клиента"));

    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public ClientAutoCompleteBox()
    {
        _clientService = ServiceLocator.GetService<IClientService>();
        _ = LoadClientsAsync();
        
        InitializeComponent();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        var grid = new Grid();
        
        _inputBox = new TextBox
        {
            Padding = new Thickness(10),
            FontSize = 14,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Background = Brushes.White
        };
        grid.Children.Add(_inputBox);
        
        this.Content = grid;
        
        InitializePopup();
    }

    private void InitializePopup()
    {
        _popup = new Popup
        {
            PlacementTarget = _inputBox,
            Placement = PlacementMode.Bottom,
            AllowsTransparency = true,
            StaysOpen = true,
            VerticalOffset = 2
        };

        _listBox = new ListBox
        {
            MaxHeight = 250,
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            ItemContainerStyle = new Style(typeof(ListBoxItem))
            {
                Setters = { new Setter(FrameworkElement.CursorProperty, Cursors.Hand) }
            },
            DisplayMemberPath = "FullName",
        };
        
        _listBox.SelectionChanged += ListBox_SelectionChanged;
        _listBox.MouseLeftButtonUp += ListBox_MouseLeftButtonUp;
        _popup.Child = _listBox;
    }

    private void SetupEventHandlers()
    {
        _inputBox.TextChanged += OnTextChanged;
        _inputBox.PreviewKeyDown += OnKeyDown;
        _inputBox.LostFocus += (s, e) =>
        {
            _ = Task.Delay(100).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => _popup.IsOpen = false);
            });
        };
    }

    private async Task LoadClientsAsync()
    {
        try
        {
            _allClients = (await _clientService.GetAllClientsAsync()).ToList();
        }
        catch { }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // Если текст изменён программно – игнорируем фильтрацию
        if (_isProgrammaticUpdate)
            return;

        var query = _inputBox.Text.Trim();

        if (string.IsNullOrEmpty(query))
        {
            _popup.IsOpen = false;
            _selectedClient = null;
            _listBox.SelectedIndex = -1;
            _listBox.ItemsSource = null;
            _hasMatches = false;
            return;
        }

        _filteredClients = FilterAndSortClients(query).Take(10).ToList();

        if (_filteredClients.Any())
        {
            _hasMatches = true;
            _listBox.ItemsSource = _filteredClients;
            _listBox.SelectedIndex = 0;
            _popup.IsOpen = true;
        }
        else
        {
            _hasMatches = false;
            _listBox.ItemsSource = null;
            _listBox.SelectedIndex = -1;
            _popup.IsOpen = false;
        }
    }

    private IEnumerable<Client> FilterAndSortClients(string query)
    {
        var lowerQuery = query.ToLower();
        
        var matches = _allClients.Select(c => new
        {
            Client = c,
            FullName = c.FullName.ToLower(),
            Score = CalculateMatchScore(c, lowerQuery)
        })
        .Where(x => x.Score > 0)
        .OrderBy(x => x.Score)
        .ThenBy(x => x.Client.FullName)
        .Select(x => x.Client)
        .ToList();

        return matches;
    }

    private int CalculateMatchScore(Client client, string query)
    {
        var fullName = client.FullName.ToLower();
        var score = 0;
        
        if (fullName == query)
            return int.MaxValue;
        
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var queryParts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        int exactPartMatches = 0;
        int partialMatches = 0;
        int positionBonus = int.MaxValue;
        
        foreach (var qPart in queryParts)
        {
            bool found = false;
            int bestPosition = int.MaxValue;
            
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                
                if (part == qPart)
                {
                    exactPartMatches++;
                    bestPosition = i;
                    found = true;
                    break;
                }
                else if (part.StartsWith(qPart) || part.Contains(qPart))
                {
                    partialMatches++;
                    if (i < bestPosition)
                        bestPosition = i;
                    found = true;
                }
            }
            
            if (!found)
                return -1;
            
            if (bestPosition < positionBonus)
                positionBonus = bestPosition;
        }
        
        score = exactPartMatches * 100 + partialMatches * 10;
        if (positionBonus != int.MaxValue)
            score += (10 - positionBonus);
        
        return score;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (_popup.IsOpen)
                {
                    if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                        _listBox.SelectedIndex++;
                    else
                        _listBox.SelectedIndex = 0;
                    e.Handled = true;
                }
                break;
                
            case Key.Up:
                if (_popup.IsOpen)
                {
                    if (_listBox.SelectedIndex > 0)
                        _listBox.SelectedIndex--;
                    else
                        _listBox.SelectedIndex = _listBox.Items.Count - 1;
                    e.Handled = true;
                }
                break;
                
            case Key.Enter:
                if (_listBox.SelectedItem is Client c)
                {
                    SelectClient(c);
                    e.Handled = true;
                }
                else if (_filteredClients.Count == 0 && _onCreateClient != null)
                {
                    _ = CreateNewClientAsync();
                    e.Handled = true;
                }
                break;
                
            case Key.Escape:
                _popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_listBox.SelectedItem is Client c)
        {
            SelectClient(c);
            _popup.IsOpen = false;
        }
    }

    private void SelectClient(Client client)
    {
        _selectedClient = client;
        _isProgrammaticUpdate = true;
        _inputBox.Text = client.FullName;
        _isProgrammaticUpdate = false;
        _inputBox.CaretIndex = _inputBox.Text.Length;
        _inputBox.Focus();
        _ = _onClientSelected?.Invoke(client);
        _popup.IsOpen = false;
    }

    private async Task CreateNewClientAsync()
    {
        var clientName = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(clientName))
            return;

        if (!PermissionChecker.CanCreate(PermissionCategory.Clients))
        {
            MessageBox.Show("Недостаточно прав для создания клиента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

    public void SetClients(List<Client> clients)
    {
        _allClients = clients;
    }

    public void Clear()
    {
        _isProgrammaticUpdate = true;
        _inputBox.Text = "";
        _isProgrammaticUpdate = false;
        _selectedClient = null;
        _popup.IsOpen = false;
    }

    public Client? SelectedClient => _selectedClient;

    public string InputText
    {
        get => _inputBox.Text;
        set
        {
            _isProgrammaticUpdate = true;
            _inputBox.Text = value;
            _inputBox.CaretIndex = value.Length;
            _isProgrammaticUpdate = false;
        }
    }
}
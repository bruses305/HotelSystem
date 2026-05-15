using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using HotelSystem.Services;
using HotelSystem.Helpers;

namespace HotelSystem.Controls;

/// <summary>
/// Универсальный компонент автодополнения для любых сущностей
/// Заменяет ClientAutoCompleteBox с поддержкой кастомизации
/// </summary>
public class GenericAutoCompleteBox<T> : UserControl where T : class
{
    private TextBox _inputBox = null!;
    private Popup _popup = null!;
    private ListBox _listBox = null!;
    private List<T> _allItems = new();
    private List<T> _filteredItems = new();
    private T? _selectedItem;
    private readonly IClientService _clientService;
    private Func<T?, Task>? _onItemSelected;
    private Func<Task<T?>?>? _onCreateItem;
    
    #region DependencyProperty
    
    public static readonly DependencyProperty WatermarkProperty =
        DependencyProperty.Register(nameof(Watermark), typeof(string), 
            typeof(GenericAutoCompleteBox<T>), new PropertyMetadata("Введите значение"));
    
    public string Watermark
    {
        get => (string)GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string),
            typeof(GenericAutoCompleteBox<T>), new PropertyMetadata(""));
    
    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }
    
    public static readonly DependencyProperty IsCreateEnabledProperty =
        DependencyProperty.Register(nameof(IsCreateEnabled), typeof(bool),
            typeof(GenericAutoCompleteBox<T>), new PropertyMetadata(true));
    
    public bool IsCreateEnabled
    {
        get => (bool)GetValue(IsCreateEnabledProperty);
        set => SetValue(IsCreateEnabledProperty, value);
    }
    
    #endregion
    
    public T? SelectedItem => _selectedItem;
    public string InputText => _inputBox.Text;
    
    public GenericAutoCompleteBox()
    {
        _clientService = ServiceLocator.GetService<IClientService>();
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
            }
        };
        
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            _listBox.DisplayMemberPath = DisplayMemberPath;
        }
        
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
    
    private async void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = _inputBox.Text.Trim();
        
        if (string.IsNullOrEmpty(query))
        {
            _popup.IsOpen = false;
            _selectedItem = null;
            _listBox.SelectedIndex = -1;
            _listBox.ItemsSource = null;
            return;
        }
        
        _filteredItems = FilterItems(query).Take(10).ToList();
        
        if (_filteredItems.Any())
        {
            _listBox.ItemsSource = _filteredItems;
            _listBox.SelectedIndex = 0;
            _popup.IsOpen = true;
        }
        else
        {
            _listBox.ItemsSource = null;
            _listBox.SelectedIndex = -1;
            _popup.IsOpen = false;
        }
    }
    
    protected virtual IEnumerable<T> FilterItems(string query)
    {
        var lowerQuery = query.ToLower();
        return _allItems.Where(item => GetDisplayValue(item)?.ToLower().Contains(lowerQuery) == true);
    }
    
    protected virtual string? GetDisplayValue(T item)
    {
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            var property = item.GetType().GetProperty(DisplayMemberPath);
            return property?.GetValue(item)?.ToString();
        }
        return item.ToString();
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
                if (_listBox.SelectedItem is T selected)
                {
                    SelectItem(selected);
                    e.Handled = true;
                }
                else if (IsCreateEnabled && _onCreateItem != null && _filteredItems.Count == 0)
                {
                    _ = CreateNewItemAsync();
                    e.Handled = true;
                }
                break;
                
            case Key.Escape:
                _popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }
    
    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Только навигация
    }
    
    private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_listBox.SelectedItem is T selected)
        {
            SelectItem(selected);
            _popup.IsOpen = false;
        }
    }
    
    private void SelectItem(T item)
    {
        _selectedItem = item;
        var displayValue = GetDisplayValue(item) ?? "";
        _inputBox.Text = displayValue;
        _inputBox.CaretIndex = _inputBox.Text.Length;
        _inputBox.Focus();
        _ = _onItemSelected?.Invoke(item);
        _popup.IsOpen = false;
    }
    
    private async Task CreateNewItemAsync()
    {
        if (!IsCreateEnabled || _onCreateItem == null)
            return;
        
        var newItem = await _onCreateItem?.Invoke();
        
        if (newItem != null)
        {
            _allItems.Add(newItem);
            SelectItem(newItem);
        }
    }
    
    #region Public Methods
    
    public void SetItemsSource(List<T> items)
    {
        _allItems = items ?? new List<T>();
    }
    
    public void SetItemSelectedHandler(Func<T?, Task> handler)
    {
        _onItemSelected = handler;
    }
    
    public void SetCreateItemHandler(Func<Task<T?>?> handler)
    {
        _onCreateItem = handler;
    }
    
    public void Clear()
    {
        _inputBox.Text = "";
        _selectedItem = null;
        _popup.IsOpen = false;
    }
    
    #endregion
}
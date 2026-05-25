using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HotelSystem.Helpers;
using HotelSystem.Models.Entities;

namespace HotelSystem.Views;

public partial class RoomProfitIncreaseDialog : Window
{
    private readonly List<Room> _selectedRooms;
    private decimal _percentage = 100m;
    
    public List<Room> RoomsToUpdate { get; private set; } = new();
    
    public RoomProfitIncreaseDialog(List<Room> selectedRooms)
    {
        InitializeComponent();
        _selectedRooms = selectedRooms;
        SelectedCountText.Text = $"Выбрано номеров: {selectedRooms.Count}";
        
        PercentageTextBox.TextChanged += PercentageTextBox_TextChanged;
        PercentageTextBox.LostFocus += PercentageTextBox_LostFocus;
        
        UpdatePreview();
    }

    private void PercentageTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(PercentageTextBox.Text, out var percent))
        {
            _percentage = percent;
            UpdatePreview();
        }
    }

    private void PercentageTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(PercentageTextBox.Text, out var percent) && percent > 0)
        {
            PercentageTextBox.Text = percent.ToString("F0");
        }
        else
        {
            PercentageTextBox.Text = "100";
            _percentage = 100m;
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        ChangesPanel.Children.Clear();
        
        if (_percentage <= 0)
        {
            InfoText.Text = "Пожалуйста, введите положительное значение процента";
            ApplyButton.IsEnabled = false;
            return;
        }
        
        ApplyButton.IsEnabled = true;
        InfoText.Text = $"{_percentage}% = прибыль умножится на {_percentage / 100:F2}";
        
        foreach (var room in _selectedRooms)
        {
            var oldProfit = room.Profit;
            var newProfit = Math.Round(oldProfit * _percentage / 100m, 2);
            var oldPrice = room.Cost + oldProfit;
            var newPrice = room.Cost + newProfit;
            
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            Grid.SetColumn(grid, 0);
            
            // Название и старая цена
            var leftStack = new StackPanel();
            var nameText = new TextBlock
            {
                Text = room.Name,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            };
            leftStack.Children.Add(nameText);
            
            var oldPriceText = new TextBlock
            {
                Text = $"Цена: {oldPrice:F0} {AppConstants.Currency} (прибыль: {oldProfit:F0} {AppConstants.Currency})",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 2, 0, 0)
            };
            leftStack.Children.Add(oldPriceText);
            
            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);
            
            // Стрелка
            var arrowText = new TextBlock
            {
                Text = "→",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(arrowText, 2);
            grid.Children.Add(arrowText);
            
            // Новая цена
            var rightStack = new StackPanel();
            var newPriceText = new TextBlock
            {
                Text = $"Цена: {newPrice:F0} {AppConstants.Currency} (прибыль: {newProfit:F0} {AppConstants.Currency})",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            rightStack.Children.Add(newPriceText);
            
            var diff = newProfit - oldProfit;
            var diffText = new TextBlock
            {
                Text = diff > 0 ? $"+{diff:F0} "+AppConstants.Currency : $"{diff:F0} "+AppConstants.Currency,
                FontSize = 12,
                Foreground = diff > 0 ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) : new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            rightStack.Children.Add(diffText);
            
            Grid.SetColumn(rightStack, 3);
            grid.Children.Add(rightStack);
            
            border.Child = grid;
            ChangesPanel.Children.Add(border);
        }
        
        // Автопрокрутка
        if (ChangesPanel.Parent is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(PercentageTextBox.Text, out var percent) && percent > 0)
        {
            _percentage = percent;
            RoomsToUpdate = _selectedRooms.Select(room =>
            {
                return new Room
                {
                    Id = room.Id,
                    Name = room.Name,
                    Cost = room.Cost,
                    Profit = Math.Round(room.Profit * _percentage / 100m, 2),
                    Area = room.Area,
                    Capacity = room.Capacity,
                    Type = room.Type,
                    Status = room.Status,
                    Description = room.Description
                };
            }).ToList();
            
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

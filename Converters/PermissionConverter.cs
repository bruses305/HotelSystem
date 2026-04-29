using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HotelSystem.Models.Entities;
using HotelSystem.Services;

namespace HotelSystem.Converters;

/// <summary>
/// Конвертер для привязки видимости элемента к праву доступа
/// </summary>
public class PermissionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PermissionCategory category)
            return Visibility.Collapsed;
        
        if (parameter is not PermissionType type)
            return Visibility.Collapsed;
        
        var hasPermission = PermissionChecker.HasPermission(category, type);
        return hasPermission ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Конвертер для привязки видимости элемента к праву доступа (инвертированный)
/// </summary>
public class InvertedPermissionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PermissionCategory category)
            return Visibility.Collapsed;
        
        if (parameter is not PermissionType type)
            return Visibility.Collapsed;
        
        var hasPermission = PermissionChecker.HasPermission(category, type);
        return hasPermission ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
using System.Windows;

namespace HotelSystem.Helpers;

/// <summary>
/// Статический помощник для отображения сообщений
/// Упрощает вызов MessageBox с единым стилем
/// </summary>
public static class MessageBoxHelper
{
    public static void ShowError(string message, string title = "Ошибка")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    public static void ShowWarning(string message, string title = "Предупреждение")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    
    public static void ShowInfo(string message, string title = "Информация")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    public static bool ShowConfirmation(string message, string title = "Подтверждение")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
    
    public static MessageBoxResult Show(string message, string title = "", 
        MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
    {
        return MessageBox.Show(message, title, buttons, image);
    }
}
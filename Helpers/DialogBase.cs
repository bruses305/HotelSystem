using System;
using System.ComponentModel;
using System.Windows;

namespace HotelSystem.Helpers;

/// <summary>
/// Базовый класс для всех диалоговых окон
/// Убирает дублирование логики сохранения/отмены/подтверждения
/// </summary>
public abstract class DialogBase : Window
{
    protected bool _isSaved;
    protected bool _isCancelled;
    
    public event EventHandler? DialogCancelled;
    public event EventHandler? DialogSaved;
    
    protected DialogBase()
    {
        Closing += OnClosing;
    }
    
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isSaved) return;
        
        // Если отмена явно вызвана - не спрашиваем
        if (_isCancelled) return;
        
        if (!HasChanges) return;
        
        var result = MessageBox.Show(
            "Есть несохранённые изменения. Сохранить?",
            "Подтверждение",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        
        switch (result)
        {
            case MessageBoxResult.Yes:
                // Сохраняем и закрываем
                try
                {
                    Save();
                    _isSaved = true;
                    DialogSaved?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true;
                }
                break;
                
            case MessageBoxResult.No:
                // Закрыть без сохранения
                _isCancelled = true;
                break;
                
            case MessageBoxResult.Cancel:
                // Оставить окно открытым
                e.Cancel = true;
                break;
        }
    }
    
    /// <summary>
    /// Проверяет наличие изменений (реализуется в подклассе)
    /// </summary>
    protected abstract bool HasChanges { get; }
    
    /// <summary>
    /// Метод сохранения (реализуется в подклассе)
    /// </summary>
    protected abstract void Save();
    
    /// <summary>
    /// Метод отмены (может переопределяться)
    /// </summary>
    protected virtual void Cancel()
    {
        _isCancelled = true;
        DialogCancelled?.Invoke(this, EventArgs.Empty);
        Close();
    }
    
    /// <summary>
    /// Безопасное закрытие без запроса сохранения
    /// </summary>
    protected void CloseWithoutPrompt()
    {
        _isCancelled = true;
        Close();
    }
    
    /// <summary>
    /// Уведомляет об успешном сохранении
    /// </summary>
    protected void MarkAsSaved()
    {
        _isSaved = true;
        DialogSaved?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Базовый класс для диалогов с результатом
/// </summary>
public abstract class DialogBase<T> : DialogBase where T : class
{
    public T? Result { get; protected set; }
    
    protected DialogBase() : base()
    {
    }
}
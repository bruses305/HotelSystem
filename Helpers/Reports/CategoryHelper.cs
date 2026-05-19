using HotelSystem.Models.Entities;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Вспомогательный класс для категоризации транзакций
/// </summary>
public static class CategoryHelper
{
    /// <summary>
    /// Получить конкретизированную категорию для отображения
    /// </summary>
    public static string GetDisplayCategory(Transaction tx, Service? service = null)
    {
        string category = tx.Category.ToString();
        string desc = tx.Description?.ToLower() ?? "";
        
        // Конкретизация доходов
        if (tx.Category == TransactionCategory.Дополнительная_услуга && service != null)
        {
            return $"Услуга: {service.Name}";
        }
        
        // Конкретизация расходов - Коммунальные услуги
        if (tx.Category == TransactionCategory.Коммунальные_услуги)
        {
            if (desc.Contains("вода")) return "Расход: Вода";
            if (desc.Contains("электричеств")) return "Расход: Электричество";
            if (desc.Contains("интернет")) return "Расход: Интернет";
            return "Расход: Коммунальные услуги";
        }
        
        // Конкретизация расходов - Обслуживание
        if (tx.Category == TransactionCategory.Обслуживание)
        {
            if (desc.Contains("уборка")) return "Расход: Уборка";
            if (desc.Contains("ремонт")) return "Расход: Ремонт";
            return "Расход: Обслуживание";
        }
        
        // Конкретизация расходов - Зарплата
        if (tx.Category == TransactionCategory.Зарплата)
        {
            return "Расход: Зарплата";
        }
        
        // Конкретизация расходов - Закупки
        if (tx.Category == TransactionCategory.Закупки)
        {
            return "Расход: Закупки";
        }
        
        return category;
    }
    
    /// <summary>
    /// Получить конкретизированную категорию для группировки расходов
    /// </summary>
    public static string GetExpenseCategoryKey(Transaction tx)
    {
        string desc = tx.Description?.ToLower() ?? "";
        
        if (tx.Category == TransactionCategory.Коммунальные_услуги)
        {
            if (desc.Contains("вода")) return "Расход: Вода";
            if (desc.Contains("электричеств")) return "Расход: Электричество";
            if (desc.Contains("интернет")) return "Расход: Интернет";
            return "Расход: Коммунальные услуги";
        }
        
        if (tx.Category == TransactionCategory.Обслуживание)
        {
            if (desc.Contains("уборка")) return "Расход: Уборка";
            if (desc.Contains("ремонт")) return "Расход: Ремонт";
            return "Расход: Обслуживание";
        }
        
        if (tx.Category == TransactionCategory.Зарплата)
            return "Расход: Зарплата";
        
        if (tx.Category == TransactionCategory.Закупки)
            return "Расход: Закупки";
        
        return tx.Category.ToString();
    }
}

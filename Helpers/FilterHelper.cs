using System;
using System.Collections.Generic;
using System.Linq;

namespace HotelSystem.Helpers;

/// <summary>
/// Универсальный помощник для фильтрации и поиска данных
/// Заменяет повторяющуюся логику в различных Views
/// </summary>
public static class FilterHelper
{
    /// <summary>
    /// Фильтрует коллекцию по текстовому запросу
    /// </summary>
    public static IEnumerable<T> FilterBySearch<T>(
        this IEnumerable<T> source,
        Func<T, string?> textSelector,
        string? searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return source;
        
        var search = searchQuery.Trim().ToLower();
        return source.Where(item => 
        {
            var text = textSelector(item)?.ToLower() ?? "";
            return text.Contains(search);
        });
    }
    
    /// <summary>
    /// Фильтрует по диапазону дат
    /// </summary>
    public static IQueryable<T> FilterByDateRange<T>(
        this IQueryable<T> query,
        Func<T, DateTime> dateSelector,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            var start = startDate.Value;
            query = query.Where(item => dateSelector(item) >= start);
        }
        
        if (endDate.HasValue)
        {
            var end = endDate.Value.AddDays(1).AddSeconds(-1);
            query = query.Where(item => dateSelector(item) <= end);
        }
        
        return query;
    }
    
    /// <summary>
    /// Фильтрует по значению enum
    /// </summary>
    public static IQueryable<T> FilterByEnum<T, TEnum>(
        this IQueryable<T> query,
        Func<T, TEnum> enumSelector,
        TEnum? value) where TEnum : struct, Enum
    {
        if (value.HasValue)
        {
            var val = value.Value;
            query = query.Where(item => enumSelector(item).Equals(val));
        }
        
        return query;
    }
    
    /// <summary>
    /// Сортирует по приоритету совпадений
    /// </summary>
    public static IEnumerable<T> SortByPriority<T>(
        this IEnumerable<T> source,
        Func<T, string?> valueSelector,
        string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return source;
        
        var query = searchQuery.Trim().ToLower();
        
        return source.OrderBy(item =>
        {
            var value = valueSelector(item)?.ToLower() ?? "";
            if (value == query) return 0;
            if (value.StartsWith(query)) return 1;
            if (value.Contains(query)) return 2;
            return 3;
        });
    }
}

/// <summary>
/// Конфигурация фильтра
/// </summary>
public class FilterConfig
{
    public string? SearchQuery { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public object? StatusFilter { get; set; }
    public int? IdFilter { get; set; }
    
    public bool HasFilters => 
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        StartDate.HasValue ||
        EndDate.HasValue ||
        StatusFilter != null ||
        IdFilter.HasValue;
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HotelSystem.Models;
using HotelSystem.Models.Entities;
using HotelSystem.Repositories;

namespace HotelSystem.Services;

public interface IExpensePriceUpdateService
{
    Task<UpdateResult> UpdatePriceAsync(Expense expense);
    Task<UpdateResult> CheckStructureChangeAsync(Expense expense);
}

public class UpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? NewValue { get; set; }
    public bool StructureChanged { get; set; }
    public string OldClassSelector { get; set; } = string.Empty;
    public string NewClassSelector { get; set; } = string.Empty;
}

public class ExpensePriceUpdateService : IExpensePriceUpdateService
{
    private readonly IPriceParseService _parseService;

    public ExpensePriceUpdateService(IPriceParseService parseService)
    {
        _parseService = parseService;
    }

    public async Task<UpdateResult> UpdatePriceAsync(Expense expense)
    {
        var result = new UpdateResult();

        if (string.IsNullOrEmpty(expense.PriceSourceJson))
        {
            result.Message = "Парсинг не настроен";
            return result;
        }

        var source = JsonConvert.DeserializeObject<ParsingSource>(expense.PriceSourceJson);
        if (source == null || string.IsNullOrEmpty(source.Url) || string.IsNullOrEmpty(source.XPath))
        {
            result.Message = "Некорректные настройки парсинга";
            return result;
        }

        var parseResult = await _parseService.FetchPriceAsync(source.Url, source.XPath);

        switch (parseResult.Type)
        {
            case ParseResultType.Success:
                source.LastParsedValue = parseResult.Value;
                source.LastSuccessfulParse = DateTime.Now;
                
                // Проверка изменения структуры
                if (!string.IsNullOrEmpty(source.ClassBasedSelector) && 
                    source.ClassBasedSelector != parseResult.ClassBasedSelector)
                {
                    result.StructureChanged = true;
                    result.OldClassSelector = source.ClassBasedSelector;
                    result.NewClassSelector = parseResult.ClassBasedSelector;
                    result.Message = "Структура сайта изменилась!";
                }
                else
                {
                    result.Message = "Цена обновлена успешно";
                }
                
                result.Success = true;
                result.NewValue = parseResult.Value;
                
                // Сохраняем обновлённый источник
                expense.PriceSourceJson = JsonConvert.SerializeObject(source);
                break;
                
            case ParseResultType.NoInternet:
                result.Message = "Нет подключения к интернету";
                break;
            case ParseResultType.SiteUnavailable:
                result.Message = "Сайт недоступен";
                break;
            case ParseResultType.ValueNotFound:
                result.Message = "Значение не найдено (XPath)";
                break;
            case ParseResultType.StructureChanged:
                result.Message = "Структура сайта изменилась";
                result.StructureChanged = true;
                break;
            default:
                result.Message = parseResult.ErrorMessage;
                break;
        }

        return result;
    }

    public async Task<UpdateResult> CheckStructureChangeAsync(Expense expense)
    {
        var result = new UpdateResult();

        if (string.IsNullOrEmpty(expense.PriceSourceJson))
            return result;

        var source = JsonConvert.DeserializeObject<ParsingSource>(expense.PriceSourceJson);
        if (source == null || string.IsNullOrEmpty(source.ClassBasedSelector))
            return result;

        var parseResult = await _parseService.FetchPriceAsync(source.Url, source.XPath);

        if (parseResult.Type == ParseResultType.Success)
        {
            if (source.ClassBasedSelector != parseResult.ClassBasedSelector)
            {
                result.StructureChanged = true;
                result.OldClassSelector = source.ClassBasedSelector;
                result.NewClassSelector = parseResult.ClassBasedSelector;
                result.Message = "Структура сайта изменилась. Рекомендуется проверить XPath.";
            }
            else
            {
                result.Message = "Структура сайта не изменилась";
            }
            result.Success = true;
        }

        return result;
    }
}
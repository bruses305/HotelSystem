using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HotelSystem.Models;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace HotelSystem.Services;

public interface IPriceParseService
{
    Task<ParseResult> FetchPriceAsync(string url, string xpath);
}

public class PriceParseService : IPriceParseService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<ParseResult> FetchPriceAsync(string url, string xpath)
    {
        try
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return ParseResult.Error(ParseResultType.NoInternet, "Нет подключения к интернету");

            var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return ParseResult.Error(ParseResultType.SiteUnavailable, "Сайт недоступен");

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            if (IsProbablyBinary(html))
                return ParseResult.Error(ParseResultType.DataHashed, "Получены некорректные (возможно, зашифрованные) данные");
            
            var node = doc.DocumentNode.SelectSingleNode(xpath);
            if (node == null)
                return ParseResult.Error(ParseResultType.ValueNotFound, $"Значение не найдено (XPath)");

            string rawText = node.InnerText?.Trim() ?? "";
            decimal price = ExtractDecimal(rawText);

            string classBased = GenerateClassBasedSelector(node);

            return ParseResult.Success(price, classBased);
        }
        catch (HttpRequestException ex)
        {
            return ParseResult.Error(ParseResultType.SiteUnavailable, $"Сайт недоступен: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ParseResult.Error(ParseResultType.UnknownError, $"Ошибка: {ex.Message}");
        }
    }

    private static decimal ExtractDecimal(string text)
    {
        var cleaned = Regex.Replace(text, @"[^\d,.\-]", "");
        cleaned = cleaned.Replace(",", ".");
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;
        return 0m;
    }

    private static string GenerateClassBasedSelector(HtmlNode node)
    {
        var parts = new System.Collections.Generic.List<string>();
        var current = node;
        while (current != null && parts.Count < 5)
        {
            string tag = current.Name;
            string cls = current.GetAttributeValue("class", "");
            string id = current.GetAttributeValue("id", "");
            if (!string.IsNullOrEmpty(id))
                parts.Insert(0, $"{tag}#{id}");
            else if (!string.IsNullOrEmpty(cls))
                parts.Insert(0, $"{tag}.{cls.Replace(' ', '.')}");
            else
                parts.Insert(0, tag);
            current = current.ParentNode;
            if (current?.Name == "body") break;
        }
        return string.Join(" > ", parts);
    }
    
    private bool IsProbablyBinary(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 100) 
            return false; // короткие строки не проверяем, возможно, это просто короткая фраза

        int printableCount = 0;
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                printableCount++;
        }
        double ratio = (double)printableCount / text.Length;
        return ratio < 0.8; // если меньше 80% печатных символов – подозрительно
    }
}
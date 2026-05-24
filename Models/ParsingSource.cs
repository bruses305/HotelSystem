using System;

namespace HotelSystem.Models;

public class ParsingSource
{
    public string Url { get; set; } = string.Empty;
    public string XPath { get; set; } = string.Empty;
    public string ClassBasedSelector { get; set; } = string.Empty;
    public DateTime LastSuccessfulParse { get; set; }
    public decimal LastParsedValue { get; set; }
}

public enum ParseResultType
{
    Success,
    NoInternet,
    SiteUnavailable,
    DataHashed,
    ValueNotFound,
    StructureChanged,
    UnknownError
}

public class ParseResult
{
    public ParseResultType Type { get; set; }
    public decimal Value { get; set; }
    public string ClassBasedSelector { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public static ParseResult Success(decimal value, string classBasedSelector) =>
        new() { Type = ParseResultType.Success, Value = value, ClassBasedSelector = classBasedSelector };

    public static ParseResult Error(ParseResultType type, string message = "") =>
        new() { Type = type, ErrorMessage = message };
}
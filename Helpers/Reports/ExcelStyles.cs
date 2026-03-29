using ClosedXML.Excel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using OxyPlot.Axes;

namespace HotelSystem.Helpers.Reports;

/// <summary>
/// Вспомогательный класс для работы со стилями Excel и экспорта графиков
/// </summary>
public static class ExcelStyles
{
    // Цвета для чередования строк
    private static readonly XLColor AlternateRowColor = XLColor.FromHtml("#F8F9FA");
    private static readonly XLColor HeaderColor = XLColor.FromHtml("#2C3E50");
    private static readonly XLColor HeaderFontColor = XLColor.White;
    
    /// <summary>
    /// Добавить строку заголовков
    /// </summary>
    public static void AddHeaderRow(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = HeaderColor;
            cell.Style.Font.FontColor = HeaderFontColor;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }
    
    /// <summary>
    /// Добавить строку данных (7 колонок)
    /// </summary>
    public static void AddDataRow(IXLWorksheet sheet, int row, bool alternateRow,
        string date, string type, string category, string room, string client, 
        string description, double amount, bool isIncome)
    {
        var bgColor = alternateRow ? AlternateRowColor : XLColor.White;
        
        SetCellValue(sheet, row, 1, date, bgColor);
        SetCellValue(sheet, row, 2, type, bgColor);
        SetCellValue(sheet, row, 3, category, bgColor);
        SetCellValue(sheet, row, 4, room, bgColor);
        SetCellValue(sheet, row, 5, client, bgColor);
        SetCellValue(sheet, row, 6, description, bgColor);
        
        var amountCell = sheet.Cell(row, 7);
        amountCell.Value = amount;
        amountCell.Style.NumberFormat.Format = "#,##0";
        amountCell.Style.Fill.BackgroundColor = bgColor;
        amountCell.Style.Font.FontColor = isIncome ? XLColor.Green : XLColor.Red;
        
        // Обводка
        for (int i = 1; i <= 7; i++)
        {
            sheet.Cell(row, i).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            sheet.Cell(row, i).Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }
    
    /// <summary>
    /// Добавить строку данных (упрощённый вариант)
    /// </summary>
    public static void AddDataRowSimple(IXLWorksheet sheet, int row, bool alternateRow, params object[] values)
    {
        var bgColor = alternateRow ? AlternateRowColor : XLColor.White;
        
        for (int i = 0; i < values.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            if (values[i] is double d)
            {
                cell.Value = d;
                cell.Style.NumberFormat.Format = "#,##0";
            }
            else
            {
                cell.Value = values[i]?.ToString() ?? "";
            }
            cell.Style.Fill.BackgroundColor = bgColor;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        }
    }
    
    /// <summary>
    /// Добавить строку с итоговой суммой
    /// </summary>
    public static void AddTotalRow(IXLWorksheet sheet, int row, string label, double value, int valueColumn)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        
        var amountCell = sheet.Cell(row, valueColumn);
        amountCell.Value = value;
        amountCell.Style.Font.Bold = true;
        amountCell.Style.NumberFormat.Format = "#,##0";
    }
    
    private static void SetCellValue(IXLWorksheet sheet, int row, int col, object value, XLColor bgColor)
    {
        var cell = sheet.Cell(row, col);
        cell.Value = value?.ToString() ?? "";
        cell.Style.Fill.BackgroundColor = bgColor;
    }
    
    /// <summary>
    /// Экспортировать модель графика в PNG
    /// </summary>
    public static async Task ExportToPngAsync(PlotModel model, string filePath)
    {
        await Task.Run(() =>
        {
            try
            {
                var exportModel = new PlotModel
                {
                    Title = model.Title,
                    Background = OxyColors.White,
                    PlotAreaBorderColor = OxyColors.Black,
                    TextColor = OxyColors.Black,
                    TitleColor = OxyColors.Black
                };
                
                // Копируем оси
                foreach (var axis in model.Axes)
                {
                    if (axis is CategoryAxis catAxis)
                    {
                        var newAxis = new CategoryAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            TitleFontSize = axis.TitleFontSize,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black,
                            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                            MajorGridlineStyle = LineStyle.Solid
                        };
                        foreach (var label in catAxis.Labels)
                            newAxis.Labels.Add(label);
                        exportModel.Axes.Add(newAxis);
                    }
                    else if (axis is LinearAxis linAxis)
                    {
                        exportModel.Axes.Add(new LinearAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            TitleFontSize = axis.TitleFontSize,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black,
                            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
                            MajorGridlineStyle = LineStyle.Solid,
                            Minimum = linAxis.Minimum,
                            Maximum = linAxis.Maximum
                        });
                    }
                    else
                    {
                        exportModel.Axes.Add(new CategoryAxis
                        {
                            Position = axis.Position,
                            Key = axis.Key,
                            Title = axis.Title,
                            AxislineColor = OxyColors.Black,
                            TicklineColor = OxyColors.Black,
                            TextColor = OxyColors.Black
                        });
                    }
                }
                
                // Копируем серии
                foreach (var series in model.Series)
                {
                    if (series is PieSeries pieSeries)
                    {
                        var newPie = new PieSeries
                        {
                            StrokeThickness = pieSeries.StrokeThickness,
                            InsideLabelPosition = pieSeries.InsideLabelPosition,
                            AngleSpan = pieSeries.AngleSpan,
                            StartAngle = pieSeries.StartAngle,
                            InsideLabelColor = OxyColors.Black,
                            OutsideLabelFormat = "{1}: {2}",
                            Stroke = OxyColors.White
                        };
                        foreach (var slice in pieSeries.Slices)
                            newPie.Slices.Add(new PieSlice(slice.Label, slice.Value) { Fill = slice.Fill });
                        exportModel.Series.Add(newPie);
                    }
                    else if (series is BarSeries barSeries)
                    {
                        var newBar = new BarSeries
                        {
                            FillColor = barSeries.FillColor,
                            StrokeColor = OxyColors.Black,
                            StrokeThickness = 1
                        };
                        foreach (var item in barSeries.Items)
                            newBar.Items.Add(new BarItem { Value = item.Value });
                        exportModel.Series.Add(newBar);
                    }
                }
                
                exportModel.InvalidatePlot(true);
                
                using var stream = System.IO.File.Create(filePath);
                var exporter = new PngExporter { Width = 800, Height = 600 };
                exporter.Export(exportModel, stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting chart: {ex.Message}");
            }
        });
    }
}

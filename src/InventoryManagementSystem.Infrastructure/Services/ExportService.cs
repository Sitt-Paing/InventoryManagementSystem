using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using CsvHelper;
using InventoryManagementSystem.Application.Common.Interfaces;

namespace InventoryManagementSystem.Infrastructure.Services;

public class ExportService : IExportService
{
    private const string DefaultFontName = "Pyidaungsu";

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1", string fontName = DefaultFontName)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columnMappings = properties.ToDictionary(p => p.Name, p => p.Name);
        return ExportToExcel(data, columnMappings, sheetName, fontName);
    }

    public byte[] ExportToExcel<T>(
        IEnumerable<T> data,
        Dictionary<string, string> columnMappings,
        string sheetName = "Sheet1",
        string fontName = DefaultFontName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        var appliedFont = string.IsNullOrWhiteSpace(fontName) ? DefaultFontName : fontName;
        worksheet.Style.Font.FontName = appliedFont;

        var propMap = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => columnMappings.ContainsKey(p.Name))
            .ToDictionary(p => p.Name, p => p);

        // Render Header Row
        int colIndex = 1;
        foreach (var (_, headerText) in columnMappings)
        {
            var headerCell = worksheet.Cell(1, colIndex);
            headerCell.Value = headerText;
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Font.FontName = appliedFont;
            headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C5DCFB");
            headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            colIndex++;
        }

        // Render Data Rows
        int rowIndex = 2;
        var dataList = data as IList<T> ?? data.ToList();
        foreach (var item in dataList)
        {
            colIndex = 1;
            foreach (var (propName, _) in columnMappings)
            {
                var cell = worksheet.Cell(rowIndex, colIndex);
                cell.Style.Font.FontName = appliedFont;

                if (propMap.TryGetValue(propName, out var prop))
                {
                    var value = prop.GetValue(item);
                    if (value != null)
                    {
                        FormatAndSetCell(cell, value);
                    }
                }
                colIndex++;
            }
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToCsv<T>(IEnumerable<T> data)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(data);
        }
        return memoryStream.ToArray();
    }

    public byte[] ExportToCsv<T>(IEnumerable<T> data, Dictionary<string, string> columnMappings)
    {
        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream))
        {
            // Write Header
            writer.WriteLine(string.Join(",", columnMappings.Values.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));

            var propMap = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => columnMappings.ContainsKey(p.Name))
                .ToDictionary(p => p.Name, p => p);

            var dataList = data as IList<T> ?? data.ToList();
            foreach (var item in dataList)
            {
                var rowValues = columnMappings.Keys.Select(propName =>
                {
                    if (propMap.TryGetValue(propName, out var prop))
                    {
                        var val = prop.GetValue(item)?.ToString()?.Replace("\"", "\"\"") ?? "";
                        return $"\"{val}\"";
                    }
                    return "\"\"";
                });

                writer.WriteLine(string.Join(",", rowValues));
            }
        }
        return memoryStream.ToArray();
    }

    private static void FormatAndSetCell(IXLCell cell, object value)
    {
        switch (value)
        {
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = "dd-mmm-yyyy h:mm AM/PM";
                break;
            case DateOnly d:
                cell.Value = d.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "dd-mmm-yyyy";
                break;
            case TimeOnly t:
                cell.Value = t.ToString("h:mm tt");
                break;
            case decimal dec:
                cell.Value = dec;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case double dbl:
                cell.Value = dbl;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case float flt:
                cell.Value = flt;
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;
            case int i:
                cell.Value = i;
                cell.Style.NumberFormat.Format = "#,##0";
                break;
            case long l:
                cell.Value = l;
                cell.Style.NumberFormat.Format = "#,##0";
                break;
            case bool b:
                cell.Value = b ? "Yes" : "No";
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }
}
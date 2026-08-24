using System.Collections.Generic;
using System.IO;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IExportService
{
    Stream? ExportToExcelStreamSpecificColumns<T>(
        List<T>? list,
        KeyValuePair<string, string>[] columns,
        string sheetName = "Sheet1",
        string fontName = "Pyidaungsu");

    Stream? ExportToExcelStreamSpecificColumns<T>(
        List<T>? list,
        Dictionary<string, string> columns,
        string sheetName = "Sheet1",
        string fontName = "Pyidaungsu");

    byte[] ExportToExcel<T>(
        IEnumerable<T> data, 
        Dictionary<string, string> columnMappings, 
        string sheetName = "Sheet1", 
        string fontName = "Pyidaungsu");

    byte[] ExportToExcel<T>(
        IEnumerable<T> data, 
        string sheetName = "Sheet1", 
        string fontName = "Pyidaungsu");

    byte[] ExportToCsv<T>(IEnumerable<T> data);

    byte[] ExportToCsv<T>(IEnumerable<T> data, Dictionary<string, string> columnMappings);
}

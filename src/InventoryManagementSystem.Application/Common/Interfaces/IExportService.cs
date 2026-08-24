using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IExportService
{
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1", string fontName = "Pyidaungsu");
    
    byte[] ExportToExcel<T>(
        IEnumerable<T> data, 
        Dictionary<string, string> columnMappings, 
        string sheetName = "Sheet1", 
        string fontName = "Pyidaungsu");

    byte[] ExportToCsv<T>(IEnumerable<T> data);

    byte[] ExportToCsv<T>(IEnumerable<T> data, Dictionary<string, string> columnMappings);
}

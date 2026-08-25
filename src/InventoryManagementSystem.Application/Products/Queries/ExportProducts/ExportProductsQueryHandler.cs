using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Products.Queries.ExportProducts;

public class ExportProductsQueryHandler : IRequestHandler<ExportProductsQuery, ExportFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IExportService _exportService;

    public ExportProductsQueryHandler(IApplicationDbContext context, IExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public async Task<ExportFileDto> Handle(ExportProductsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !p.DeletedOn.HasValue);

        if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                Sku = p.Sku ?? "-",
                Barcode = p.Barcode ?? "-",
                UnitPrice = p.UnitPrice,
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                CreatedOn = p.CreatedOn,
                CreatedBy = p.CreatedBy ?? "-"
            })
            .ToListAsync(cancellationToken);

        var columnMappings = new Dictionary<string, string>
        {
            { "Name", "Product Name" },
            { "CategoryName", "Category" },
            { "Sku", "SKU" },
            { "Barcode", "Barcode" },
            { "UnitPrice", "Unit Price" },
            { "CurrentStock", "Current Stock" },
            { "ReorderLevel", "Reorder Level" },
            { "CreatedOn", "Created Date" },
            { "CreatedBy", "Created By" }
        };

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        bool isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            var csvBytes = _exportService.ExportToCsv(products, columnMappings);
            return new ExportFileDto
            {
                Content = csvBytes,
                ContentType = "text/csv; charset=utf-8",
                FileName = $"Products_{timestamp}.csv"
            };
        }

        string fontName = string.IsNullOrWhiteSpace(request.FontName) ? "Pyidaungsu" : request.FontName;
        byte[] excelBytes = _exportService.ExportToExcel(products, columnMappings, "Products", fontName);

        return new ExportFileDto
        {
            Content = excelBytes,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"Products_{timestamp}.xlsx"
        };
    }
}

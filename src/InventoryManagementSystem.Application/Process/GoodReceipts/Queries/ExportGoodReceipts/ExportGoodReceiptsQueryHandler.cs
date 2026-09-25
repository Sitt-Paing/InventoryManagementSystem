using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.ExportGoodReceipts;

public class ExportGoodReceiptsQueryHandler : IRequestHandler<ExportGoodReceiptsQuery, ExportFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IExportService _exportService;

    public ExportGoodReceiptsQueryHandler(IApplicationDbContext context, IExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public async Task<ExportFileDto> Handle(ExportGoodReceiptsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.GoodReceipts
            .AsNoTracking()
            .Include(gr => gr.Supplier)
            .Include(gr => gr.Warehouse)
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.Items)
            .Where(gr => !gr.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var keyword = request.Q.Trim().ToLower();
            query = query.Where(gr =>
                gr.ReceiptNo.ToLower().Contains(keyword) ||
                gr.ReceivedBy.ToLower().Contains(keyword) ||
                (gr.PurchaseOrder != null && gr.PurchaseOrder.PurchaseOrderNo.ToLower().Contains(keyword)) ||
                (gr.Supplier != null && gr.Supplier.CompanyName.ToLower().Contains(keyword)) ||
                (gr.Warehouse != null && gr.Warehouse.Name.ToLower().Contains(keyword)));
        }

        if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
        {
            query = query.Where(gr => gr.SupplierId == request.SupplierId.Value);
        }

        if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0)
        {
            query = query.Where(gr => gr.WarehouseId == request.WarehouseId.Value);
        }

        if (request.PurchaseOrderId.HasValue && request.PurchaseOrderId.Value != Guid.Empty)
        {
            query = query.Where(gr => gr.PurchaseOrderId == request.PurchaseOrderId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(gr => gr.Status == request.Status.Value);
        }

        if (request.ReceiptDate.HasValue)
        {
            var date = request.ReceiptDate.Value.Date;
            var nextDay = date.AddDays(1);
            query = query.Where(gr => gr.ReceiptDate >= date && gr.ReceiptDate < nextDay);
        }

        var receipts = await query
            .OrderByDescending(gr => gr.ReceiptDate)
            .Select(gr => new
            {
                ReceiptNo = gr.ReceiptNo,
                PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.PurchaseOrderNo : "-",
                SupplierName = gr.Supplier != null ? gr.Supplier.CompanyName : "-",
                WarehouseName = gr.Warehouse != null ? gr.Warehouse.Name : "-",
                ReceiptDate = gr.ReceiptDate.ToString("yyyy-MM-dd"),
                ReceivedBy = gr.ReceivedBy,
                Status = gr.Status ? "Active" : "Closed",
                TotalItems = gr.Items.Count(i => !i.DeletedOn.HasValue),
                Note = gr.Note ?? "-",
                CreatedOn = gr.CreatedOn.HasValue ? gr.CreatedOn.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                CreatedBy = gr.CreatedBy ?? "-"
            })
            .ToListAsync(cancellationToken);

        var columnMappings = new Dictionary<string, string>
        {
            { "ReceiptNo", "Receipt Number" },
            { "PurchaseOrderNo", "PO Number" },
            { "SupplierName", "Supplier" },
            { "WarehouseName", "Warehouse" },
            { "ReceiptDate", "Receipt Date" },
            { "ReceivedBy", "Received By" },
            { "Status", "Status" },
            { "TotalItems", "Total Items" },
            { "Note", "Note" },
            { "CreatedOn", "Created Date" },
            { "CreatedBy", "Created By" }
        };

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        bool isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            var csvBytes = _exportService.ExportToCsv(receipts, columnMappings);
            return new ExportFileDto
            {
                Content = csvBytes,
                ContentType = "text/csv; charset=utf-8",
                FileName = $"GoodsReceipts_{timestamp}.csv"
            };
        }

        string fontName = string.IsNullOrWhiteSpace(request.FontName) ? "Pyidaungsu" : request.FontName;
        byte[] excelBytes = _exportService.ExportToExcel(receipts, columnMappings, "GoodsReceipts", fontName);

        return new ExportFileDto
        {
            Content = excelBytes,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"GoodsReceipts_{timestamp}.xlsx"
        };
    }
}

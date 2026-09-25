using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.ExportPurchaseOrders;

public class ExportPurchaseOrdersQueryHandler : IRequestHandler<ExportPurchaseOrdersQuery, ExportFileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IExportService _exportService;

    public ExportPurchaseOrdersQueryHandler(IApplicationDbContext context, IExportService exportService)
    {
        _context = context;
        _exportService = exportService;
    }

    public async Task<ExportFileDto> Handle(ExportPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Items)
            .Where(po => !po.DeletedOn.HasValue);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var keyword = request.Q.Trim().ToLower();
            query = query.Where(po =>
                po.PurchaseOrderNo.ToLower().Contains(keyword) ||
                (po.Supplier != null && po.Supplier.CompanyName.ToLower().Contains(keyword)) ||
                (po.Warehouse != null && po.Warehouse.Name.ToLower().Contains(keyword)));
        }

        if (request.SupplierId.HasValue && request.SupplierId.Value > 0)
        {
            query = query.Where(po => po.SupplierId == request.SupplierId.Value);
        }

        if (request.WarehouseId.HasValue && request.WarehouseId.Value > 0)
        {
            query = query.Where(po => po.WarehouseId == request.WarehouseId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(po => po.Status == request.Status.Value);
        }


        if (request.OrderDate.HasValue)
        {
            var date = request.OrderDate.Value.Date;
            var nextDay = date.AddDays(1);
            query = query.Where(po => po.OrderDate >= date && po.OrderDate < nextDay);
        }

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .Select(po => new
            {
                PurchaseOrderNo = po.PurchaseOrderNo,
                SupplierName = po.Supplier != null ? po.Supplier.CompanyName : "-",
                WarehouseName = po.Warehouse != null ? po.Warehouse.Name : "-",
                OrderDate = po.OrderDate.ToString("yyyy-MM-dd"),
                ExpectedDate = po.ExpectedDate.ToString("yyyy-MM-dd"),
                Status = po.Status == PurchaseOrderStatus.Completed ? "Completed" :
                         po.Status == PurchaseOrderStatus.PartiallyReceived ? "Partially Received" :
                         po.Status == PurchaseOrderStatus.Cancelled ? "Cancelled" : "Pending",
                TotalItems = po.Items.Count(i => !i.DeletedOn.HasValue),
                TotalAmount = po.TotalAmount,
                CreatedOn = po.CreatedOn.HasValue ? po.CreatedOn.Value.ToString("yyyy-MM-dd HH:mm") : "-",
                CreatedBy = po.CreatedBy ?? "-"
            })
            .ToListAsync(cancellationToken);

        var columnMappings = new Dictionary<string, string>
        {
            { "PurchaseOrderNo", "PO Number" },
            { "SupplierName", "Supplier" },
            { "WarehouseName", "Warehouse" },
            { "OrderDate", "Order Date" },
            { "ExpectedDate", "Expected Date" },
            { "Status", "Status" },
            { "TotalItems", "Total Items" },
            { "TotalAmount", "Total Amount" },
            { "CreatedOn", "Created Date" },
            { "CreatedBy", "Created By" }
        };

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        bool isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);

        if (isCsv)
        {
            var csvBytes = _exportService.ExportToCsv(orders, columnMappings);
            return new ExportFileDto
            {
                Content = csvBytes,
                ContentType = "text/csv; charset=utf-8",
                FileName = $"PurchaseOrders_{timestamp}.csv"
            };
        }

        string fontName = string.IsNullOrWhiteSpace(request.FontName) ? "Pyidaungsu" : request.FontName;
        byte[] excelBytes = _exportService.ExportToExcel(orders, columnMappings, "PurchaseOrders", fontName);

        return new ExportFileDto
        {
            Content = excelBytes,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileName = $"PurchaseOrders_{timestamp}.xlsx"
        };
    }
}

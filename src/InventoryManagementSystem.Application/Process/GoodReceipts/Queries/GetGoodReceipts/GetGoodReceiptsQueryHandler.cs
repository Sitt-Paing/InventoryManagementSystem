using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceipts;

public class GetGoodReceiptsQueryHandler : IRequestHandler<GetGoodReceiptsQuery, PagedResult<GoodReceiptDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGoodReceiptsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<GoodReceiptDto>> Handle(GetGoodReceiptsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.GoodReceipts
            .AsNoTracking()
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

        // Sorting
        query = request.SortField?.ToLower() switch
        {
            "receiptno" => request.Order == 1 ? query.OrderBy(g => g.ReceiptNo) : query.OrderByDescending(g => g.ReceiptNo),
            "purchaseorderno" => request.Order == 1 ? query.OrderBy(g => g.PurchaseOrder!.PurchaseOrderNo) : query.OrderByDescending(g => g.PurchaseOrder!.PurchaseOrderNo),
            "suppliername" => request.Order == 1 ? query.OrderBy(g => g.Supplier!.CompanyName) : query.OrderByDescending(g => g.Supplier!.CompanyName),
            "warehousename" => request.Order == 1 ? query.OrderBy(g => g.Warehouse!.Name) : query.OrderByDescending(g => g.Warehouse!.Name),
            "receiptdate" => request.Order == 1 ? query.OrderBy(g => g.ReceiptDate) : query.OrderByDescending(g => g.ReceiptDate),
            "receivedby" => request.Order == 1 ? query.OrderBy(g => g.ReceivedBy) : query.OrderByDescending(g => g.ReceivedBy),
            "status" => request.Order == 1 ? query.OrderBy(g => g.Status) : query.OrderByDescending(g => g.Status),
            _ => request.Order == 1 ? query.OrderBy(g => g.CreatedOn) : query.OrderByDescending(g => g.CreatedOn ?? g.ReceiptDate)
        };

        var totalRecords = await query.CountAsync(cancellationToken);

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(gr => new GoodReceiptDto
            {
                Id = gr.Id,
                ReceiptNo = gr.ReceiptNo,
                WarehouseId = gr.WarehouseId,
                WarehouseName = gr.Warehouse != null ? gr.Warehouse.Name : string.Empty,
                PurchaseOrderId = gr.PurchaseOrderId,
                PurchaseOrderNo = gr.PurchaseOrder != null ? gr.PurchaseOrder.PurchaseOrderNo : string.Empty,
                SupplierId = gr.SupplierId,
                SupplierName = gr.Supplier != null ? gr.Supplier.CompanyName : string.Empty,
                SupplierCode = gr.Supplier != null ? gr.Supplier.SupplierCode : null,
                ReceiptDate = gr.ReceiptDate,
                Status = gr.Status,
                ReceivedBy = gr.ReceivedBy,
                Note = gr.Note,
                CreatedOn = gr.CreatedOn,
                CreatedBy = gr.CreatedBy,
                UpdatedOn = gr.UpdatedOn,
                UpdatedBy = gr.UpdatedBy,
                Items = gr.Items
                    .Where(i => !i.DeletedOn.HasValue)
                    .Select(i => new GoodReceiptItemDto
                    {
                        Id = i.Id,
                        GoodReceiptId = i.GoodReceiptId,
                        PurchaseOrderItemId = i.PurchaseOrderItemId,
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : string.Empty,
                        ProductSku = i.Product != null ? i.Product.Sku : null,
                        UomId = i.UomId,
                        UomName = i.Uom != null ? i.Uom.Name : string.Empty,
                        ReceivedQuantity = i.ReceivedQuantity,
                        CreatedOn = i.CreatedOn,
                        CreatedBy = i.CreatedBy
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<GoodReceiptDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

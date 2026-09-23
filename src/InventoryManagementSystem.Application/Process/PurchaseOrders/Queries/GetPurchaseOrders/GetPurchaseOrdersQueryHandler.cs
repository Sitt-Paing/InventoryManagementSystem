using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrders;

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, PagedResult<PurchaseOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PurchaseOrderDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Warehouse)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product)
            .Include(po => po.Items)
                .ThenInclude(i => i.Uom)
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

        if (request.StartDate.HasValue)
        {
            query = query.Where(po => po.OrderDate >= request.StartDate.Value.Date);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(po => po.OrderDate <= endOfDay);
        }

        // Sorting
        query = request.SortField?.ToLower() switch
        {
            "purchaseorderno" => request.Order == 1 ? query.OrderBy(p => p.PurchaseOrderNo) : query.OrderByDescending(p => p.PurchaseOrderNo),
            "suppliername" => request.Order == 1 ? query.OrderBy(p => p.Supplier!.CompanyName) : query.OrderByDescending(p => p.Supplier!.CompanyName),
            "warehousename" => request.Order == 1 ? query.OrderBy(p => p.Warehouse!.Name) : query.OrderByDescending(p => p.Warehouse!.Name),
            "orderdate" => request.Order == 1 ? query.OrderBy(p => p.OrderDate) : query.OrderByDescending(p => p.OrderDate),
            "expecteddate" => request.Order == 1 ? query.OrderBy(p => p.ExpectedDate) : query.OrderByDescending(p => p.ExpectedDate),
            "totalamount" => request.Order == 1 ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount),
            "status" => request.Order == 1 ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
            _ => request.Order == 1 ? query.OrderBy(p => p.CreatedOn) : query.OrderByDescending(p => p.CreatedOn ?? p.OrderDate)
        };

        var totalRecords = await query.CountAsync(cancellationToken);

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderDto
            {
                Id = po.Id,
                PurchaseOrderNo = po.PurchaseOrderNo,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier != null ? po.Supplier.CompanyName : string.Empty,
                SupplierCode = po.Supplier != null ? po.Supplier.SupplierCode : null,
                WarehouseId = po.WarehouseId,
                WarehouseName = po.Warehouse != null ? po.Warehouse.Name : string.Empty,
                OrderDate = po.OrderDate,
                ExpectedDate = po.ExpectedDate,
                Status = po.Status,
                TotalAmount = po.TotalAmount,
                CreatedOn = po.CreatedOn,
                CreatedBy = po.CreatedBy,
                UpdatedOn = po.UpdatedOn,
                UpdatedBy = po.UpdatedBy,
                Items = po.Items
                    .Where(i => !i.DeletedOn.HasValue)
                    .Select(i => new PurchaseOrderItemDto
                    {
                        Id = i.Id,
                        PurchaseOrderId = i.PurchaseOrderId,
                        ProductId = i.ProductId,
                        ProductName = i.Product != null ? i.Product.Name : string.Empty,
                        ProductSku = i.Product != null ? i.Product.Sku : null,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        UomId = i.UomId,
                        UomName = i.Uom != null ? i.Uom.Name : string.Empty,
                        ReceivedQuantity = i.ReceivedQuantity,
                        CreatedOn = i.CreatedOn,
                        CreatedBy = i.CreatedBy,
                        UpdatedOn = i.UpdatedOn,
                        UpdatedBy = i.UpdatedBy
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items,
            TotalRecords = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

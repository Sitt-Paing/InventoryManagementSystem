using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Products.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductByBarcode;

public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, BarcodeLookupResultDto?>
{
    private readonly IApplicationDbContext _context;

    public GetProductByBarcodeQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BarcodeLookupResultDto?> Handle(GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return null;

        var rawCode = request.Code.Trim();
        var isGuid = Guid.TryParse(rawCode, out var pGuid);

        // 1. First, search for Product by exact Barcode, SKU, or ProductId
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PurchaseUom)
            .Where(p => !p.DeletedOn.HasValue)
            .FirstOrDefaultAsync(p =>
                (p.Barcode != null && p.Barcode == rawCode) ||
                (p.Sku != null && p.Sku == rawCode) ||
                (isGuid && p.Id == pGuid),
                cancellationToken);

        // Fallback: Case-insensitive search if exact match didn't find anything
        if (product == null)
        {
            var lowerCode = rawCode.ToLower();
            product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.PurchaseUom)
                .Where(p => !p.DeletedOn.HasValue)
                .FirstOrDefaultAsync(p =>
                    (p.Barcode != null && p.Barcode.ToLower() == lowerCode) ||
                    (p.Sku != null && p.Sku.ToLower() == lowerCode),
                    cancellationToken);
        }

        // If product found, build ProductBarcodeLookupDto
        if (product != null)
        {
            return await BuildProductResultAsync(product, cancellationToken);
        }

        // 2. If Product was not found, check if this is a WarehouseLocation Barcode or LocationCode
        var location = await _context.WarehouseLocations
            .AsNoTracking()
            .Include(l => l.Warehouse)
            .Where(l => !l.DeletedOn.HasValue)
            .FirstOrDefaultAsync(l =>
                (l.Barcode != null && l.Barcode == rawCode) ||
                l.LocationCode == rawCode ||
                (l.Barcode != null && l.Barcode.ToLower() == rawCode.ToLower()) ||
                l.LocationCode.ToLower() == rawCode.ToLower(),
                cancellationToken);

        if (location != null)
        {
            return await BuildLocationResultAsync(location, cancellationToken);
        }

        return null;
    }

    private async Task<BarcodeLookupResultDto> BuildProductResultAsync(Product product, CancellationToken cancellationToken)
    {
        var productDto = new ProductBarcodeLookupDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Sku = product.Sku,
            Barcode = product.Barcode,
            Brand = product.Brand,
            CategoryName = product.Category?.Name,
            Unit = product.Unit ?? product.PurchaseUom?.Code ?? product.PurchaseUom?.Name,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            CurrentStock = product.CurrentStock,
            ReorderLevel = product.ReorderLevel,
            ReorderQuantity = product.ReorderQuantity,
            Status = product.Status,
            Description = product.Description
        };

        // A. Warehouse Stocks Summary
        var warehouseStocks = await _context.WarehouseStocks
            .AsNoTracking()
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.ProductId == product.Id && !ws.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        productDto.WarehouseStocks = warehouseStocks
            .Select(ws => new ProductWarehouseSummaryDto
            {
                WarehouseId = ws.WarehouseId,
                WarehouseName = ws.Warehouse?.Name ?? $"Warehouse #{ws.WarehouseId}",
                Quantity = ws.Quantity
            })
            .OrderBy(w => w.WarehouseName)
            .ToList();

        // B. Stock Transactions & Location Aggregations
        var transactions = await _context.StockTransactions
            .AsNoTracking()
            .Where(st => st.ProductId == product.Id && !st.DeletedOn.HasValue)
            .OrderByDescending(st => st.TransactionDate)
            .ToListAsync(cancellationToken);

        var allWarehouses = await _context.Warehouses
            .AsNoTracking()
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var allLocations = await _context.WarehouseLocations
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Aggregate stock quantities per (WarehouseId, WarehouseLocationId)
        var locationQuantities = new Dictionary<(int WarehouseId, int LocationId), decimal>();
        var lastMovementDates = new Dictionary<(int WarehouseId, int LocationId), DateTime>();

        foreach (var txn in transactions)
        {
            var key = (txn.WarehouseId, txn.WarehouseLocationId);
            if (!locationQuantities.ContainsKey(key))
            {
                locationQuantities[key] = 0;
            }
            if (!lastMovementDates.ContainsKey(key))
            {
                lastMovementDates[key] = txn.TransactionDate;
            }

            var type = (txn.TransactionType ?? "").Trim().ToUpperInvariant();
            if (type == "IN")
            {
                locationQuantities[key] += txn.Quantity;
            }
            else if (type == "OUT")
            {
                locationQuantities[key] -= txn.Quantity;
            }
            else if (type == "TRANSFER")
            {
                locationQuantities[key] -= txn.Quantity;

                if (txn.ToWarehouseId.HasValue)
                {
                    var destKey = (txn.ToWarehouseId.Value, txn.ToWarehouseLocationId ?? 0);
                    if (!locationQuantities.ContainsKey(destKey))
                    {
                        locationQuantities[destKey] = 0;
                    }
                    if (!lastMovementDates.ContainsKey(destKey))
                    {
                        lastMovementDates[destKey] = txn.TransactionDate;
                    }
                    locationQuantities[destKey] += txn.Quantity;
                }
            }
            else if (type == "ADJUSTMENT")
            {
                // In this system adjustments record the adjustment quantity or balance
                locationQuantities[key] += txn.Quantity;
            }
        }

        var locationDetails = new List<ProductLocationDetailDto>();

        // Add specific locations (LocationId > 0)
        foreach (var entry in locationQuantities.Where(e => e.Key.LocationId > 0))
        {
            var whId = entry.Key.WarehouseId;
            var locId = entry.Key.LocationId;
            var qty = entry.Value;
            lastMovementDates.TryGetValue(entry.Key, out var lastDate);

            allWarehouses.TryGetValue(whId, out var whName);
            allLocations.TryGetValue(locId, out var locEntity);

            locationDetails.Add(new ProductLocationDetailDto
            {
                WarehouseId = whId,
                WarehouseName = whName ?? locEntity?.Warehouse?.Name ?? $"Warehouse #{whId}",
                WarehouseLocationId = locId,
                LocationCode = locEntity?.LocationCode ?? $"Loc #{locId}",
                Zone = locEntity?.Zone,
                Rack = locEntity?.Rack,
                Bin = locEntity?.Bin,
                LocationBarcode = locEntity?.Barcode,
                Quantity = qty > 0 ? qty : 0,
                LastMovementDate = lastDate != default ? lastDate : (DateTime?)null,
                IsGeneralArea = false
            });
        }

        // Account for WarehouseStocks that may not have specific location assignments (LocationId == 0)
        foreach (var ws in warehouseStocks)
        {
            var allocatedInWh = locationDetails
                .Where(l => l.WarehouseId == ws.WarehouseId && !l.IsGeneralArea)
                .Sum(l => l.Quantity);

            var unallocatedQty = ws.Quantity - allocatedInWh;

            if (unallocatedQty > 0 || !locationDetails.Any(l => l.WarehouseId == ws.WarehouseId))
            {
                var generalQty = unallocatedQty > 0 ? unallocatedQty : (ws.Quantity > 0 ? ws.Quantity : 0);
                locationDetails.Add(new ProductLocationDetailDto
                {
                    WarehouseId = ws.WarehouseId,
                    WarehouseName = ws.Warehouse?.Name ?? $"Warehouse #{ws.WarehouseId}",
                    WarehouseLocationId = null,
                    LocationCode = "General Storage (Unassigned Bin)",
                    Zone = "-",
                    Rack = "-",
                    Bin = "-",
                    LocationBarcode = null,
                    Quantity = generalQty,
                    LastMovementDate = null,
                    IsGeneralArea = true
                });
            }
        }

        // Sort locations: first by Quantity descending, then by Warehouse Name, then by Location Code
        productDto.Locations = locationDetails
            .OrderByDescending(l => l.Quantity > 0)
            .ThenByDescending(l => l.Quantity)
            .ThenBy(l => l.WarehouseName)
            .ThenBy(l => l.LocationCode)
            .ToList();

        // C. Recent Movements (last 10 transactions)
        productDto.RecentMovements = transactions
            .Take(10)
            .Select(t =>
            {
                allWarehouses.TryGetValue(t.WarehouseId, out var srcWh);
                allLocations.TryGetValue(t.WarehouseLocationId, out var srcLoc);

                string? destWh = null;
                string? destLoc = null;
                if (t.ToWarehouseId.HasValue && allWarehouses.TryGetValue(t.ToWarehouseId.Value, out var dw))
                {
                    destWh = dw;
                }
                if (t.ToWarehouseLocationId.HasValue && allLocations.TryGetValue(t.ToWarehouseLocationId.Value, out var dl))
                {
                    destLoc = dl.LocationCode;
                }

                return new ProductRecentMovementDto
                {
                    Id = t.Id,
                    TransactionType = t.TransactionType,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    WarehouseName = srcWh ?? $"Warehouse #{t.WarehouseId}",
                    LocationCode = srcLoc?.LocationCode ?? (t.WarehouseLocationId > 0 ? $"Loc #{t.WarehouseLocationId}" : "General Area"),
                    ToWarehouseName = destWh,
                    ToLocationCode = destLoc,
                    UserId = t.UserId,
                    Note = t.Note
                };
            })
            .ToList();

        return new BarcodeLookupResultDto
        {
            LookupType = "Product",
            Product = productDto
        };
    }

    private async Task<BarcodeLookupResultDto> BuildLocationResultAsync(WarehouseLocation location, CancellationToken cancellationToken)
    {
        var locationDto = new LocationBarcodeLookupDto
        {
            WarehouseLocationId = location.Id,
            WarehouseId = location.WarehouseId,
            WarehouseName = location.Warehouse?.Name ?? $"Warehouse #{location.WarehouseId}",
            LocationCode = location.LocationCode,
            Zone = location.Zone,
            Rack = location.Rack,
            Bin = location.Bin,
            Barcode = location.Barcode,
            Capacity = location.Capacity,
            Status = location.Status
        };

        // Query all transactions involving this location
        var txns = await _context.StockTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Where(t => !t.DeletedOn.HasValue && (t.WarehouseLocationId == location.Id || t.ToWarehouseLocationId == location.Id))
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        var productQuantities = new Dictionary<Guid, decimal>();
        var productLastDates = new Dictionary<Guid, DateTime>();
        var productEntities = new Dictionary<Guid, Product>();

        foreach (var t in txns)
        {
            if (t.Product != null && !productEntities.ContainsKey(t.ProductId))
            {
                productEntities[t.ProductId] = t.Product;
            }

            var type = (t.TransactionType ?? "").Trim().ToUpperInvariant();

            // Source is this location
            if (t.WarehouseLocationId == location.Id)
            {
                if (!productQuantities.ContainsKey(t.ProductId))
                {
                    productQuantities[t.ProductId] = 0;
                    productLastDates[t.ProductId] = t.TransactionDate;
                }

                if (type == "IN" || type == "ADJUSTMENT")
                {
                    productQuantities[t.ProductId] += t.Quantity;
                }
                else if (type == "OUT" || type == "TRANSFER")
                {
                    productQuantities[t.ProductId] -= t.Quantity;
                }
            }

            // Destination is this location (TRANSFER)
            if (t.ToWarehouseLocationId == location.Id && type == "TRANSFER")
            {
                if (!productQuantities.ContainsKey(t.ProductId))
                {
                    productQuantities[t.ProductId] = 0;
                    productLastDates[t.ProductId] = t.TransactionDate;
                }
                productQuantities[t.ProductId] += t.Quantity;
            }
        }

        locationDto.Products = productQuantities
            .Where(p => p.Value > 0)
            .Select(p =>
            {
                productEntities.TryGetValue(p.Key, out var prod);
                productLastDates.TryGetValue(p.Key, out var lastDate);
                return new LocationProductItemDto
                {
                    ProductId = p.Key,
                    ProductName = prod?.Name ?? "Unknown Product",
                    Sku = prod?.Sku,
                    Barcode = prod?.Barcode,
                    Unit = prod?.Unit,
                    Quantity = p.Value,
                    LastMovementDate = lastDate != default ? lastDate : (DateTime?)null
                };
            })
            .OrderByDescending(p => p.Quantity)
            .ToList();

        return new BarcodeLookupResultDto
        {
            LookupType = "Location",
            Location = locationDto
        };
    }
}

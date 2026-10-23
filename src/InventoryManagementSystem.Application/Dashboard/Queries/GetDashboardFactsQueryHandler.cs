using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using InventoryManagementSystem.Application.Dashboard.Queries.GetDashboardFacts;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Dashboard.Queries;

public sealed class GetDashboardFactsQueryHandler : IRequestHandler<GetDashboardFactsQuery, DashboardFactsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardFactsQueryHandler( IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardFactsDto> Handle(GetDashboardFactsQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue)
        {
            throw new InvalidOperationException("Select a company before loading the dashboard");
        }

        var start = request.StartDate.ToDateTime(TimeOnly.MinValue);

        var endExclusive = request.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var productsQuery = _context.Products
            .AsNoTracking()
            .Where(product => !product.DeletedOn.HasValue);

        int activeProductCount = await productsQuery
            .CountAsync(
                product => product.Status,
                cancellationToken);

        int openPurchaseOrderCount = await _context.PurchaseOrders
           .AsNoTracking()
           .Where(order =>
               !order.DeletedOn.HasValue &&
               order.ApprovalStatus ==
                   PurchaseOrderApprovalStatus.Approved &&
               (
                   order.Status == PurchaseOrderStatus.Pending ||
                   order.Status ==
                       PurchaseOrderStatus.PartiallyReceived
               ))
           .CountAsync(cancellationToken);

        var productStocks = await productsQuery
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Select(product => new DashboardProductStockDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                BaseUnit = product.BaseUom.Code,
                BaseUnitSymbol = (product.BaseUom.Symbol ?? "").Trim(),
                ReorderLevel = product.ReorderLevel,

                AvailableQuantity = _context.WarehouseStocks
                    .Where(stock =>
                        stock.ProductId == product.Id &&
                        !stock.DeletedOn.HasValue)
                    .Sum(stock => (decimal?)stock.Quantity) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var dailyUsageQuery =
            from item in _context.StockIssueItems.AsNoTracking()

            join issue in _context.StockIssues.AsNoTracking()
                on item.StockIssueId equals issue.Id

            join product in productsQuery
                on item.ProductId equals product.Id

            where !item.DeletedOn.HasValue
                && !issue.DeletedOn.HasValue
                && issue.Status == StockIssueStatus.Posted
                && issue.IssueDate >= start
                && issue.IssueDate < endExclusive

            group item by new
            {
                Day = issue.IssueDate.Date,
                ProductId = product.Id,
                ProductName = product.Name,
                BaseUnit = product.BaseUom.Code,
                BaseUnitSymbol = (product.BaseUom.Symbol ?? "").Trim()
            }
            into usageGroup

            orderby usageGroup.Key.Day,
                usageGroup.Key.ProductId

            select new
            {
                usageGroup.Key.Day,
                usageGroup.Key.ProductId,
                usageGroup.Key.ProductName,
                usageGroup.Key.BaseUnit,
                usageGroup.Key.BaseUnitSymbol,
                IssuedQuantity = usageGroup.Sum(item => item.Quantity)
            };

        var dailyUsageRows = await dailyUsageQuery
          .ToListAsync(cancellationToken);

        var dailyProductUsage = dailyUsageRows
            .Select(row => new DashboardDailyUsageDto
            {
                Date = DateOnly.FromDateTime(row.Day),
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                BaseUnit = row.BaseUnit,
                BaseUnitSymbol = row.BaseUnitSymbol,
                IssuedQuantity = row.IssuedQuantity
            })
            .ToList();

        return new DashboardFactsDto
        {
            DataAsOfUtc = DateTime.UtcNow,
            SnapshotDate = DateOnly.FromDateTime(DateTime.Now),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ActiveProductCount = activeProductCount,
            OpenPurchaseOrderCount = openPurchaseOrderCount,
            ProductStocks = productStocks,
            DailyProductUsage = dailyProductUsage
        };
    }
}

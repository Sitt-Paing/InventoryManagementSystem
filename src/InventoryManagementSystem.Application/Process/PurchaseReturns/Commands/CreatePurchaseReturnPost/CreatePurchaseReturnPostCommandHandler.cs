using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnPost;

public class CreatePurchaseReturnPostCommandHandler : IRequestHandler<CreatePurchaseReturnPostCommand, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;
    public CreatePurchaseReturnPostCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }
    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnPostCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        PurchaseReturn? postedReturn = await _context.PurchaseReturns
            .Include(pr => pr.Items)
            .ThenInclude(item => item.GoodReceiptItem)
            .ThenInclude(receiptItem => receiptItem!.GoodReceipt)
            .FirstOrDefaultAsync(x => x.Id == request.PurchaseReturnId && x.CompanyId == companyId && !x.DeletedOn.HasValue, cancellationToken);

        if(postedReturn == null)
        {
            throw new KeyNotFoundException("Purchase return is not found");
        }

        if(postedReturn.Status == PurchaseReturnStatus.Posted)
        {
            var result = await _sender.Send(new GetPurchaseReturnByIdQuery(request.PurchaseReturnId),cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        if(postedReturn.Status != PurchaseReturnStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft Return can be posted.");
        }

        List<PurchaseReturnItem> items = postedReturn.Items.Where(x => !x.DeletedOn.HasValue).ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least One return is required.");
        }

        List<long> receiptItemIds = items.Select(i => i.GoodReceiptItemId).ToList();
        if (receiptItemIds.Distinct().Count() != receiptItemIds.Count)
            throw new InvalidOperationException("Each receipt item can appear only once in a return.");

        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Login is required to post a return.");
        if (!await _context.Suppliers.AnyAsync(s => s.Id == postedReturn.SupplierId && s.CompanyId == companyId
            && !s.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The return supplier is unavailable.");

        var returnedQuantities = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(item => 
            receiptItemIds.Contains(item.GoodReceiptItemId)
            && item.CompanyId == companyId
            &&  !item.DeletedOn.HasValue
            && item.PurchaseReturn!.CompanyId == companyId
            && !item.PurchaseReturn.DeletedOn.HasValue
            && item.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
            .GroupBy(item => item.GoodReceiptItemId)
            .Select(group => new
            {
                ReceiptItemId = group.Key,
                Quantity = group.Sum(item => item.ReturnQuantity)
            })
            .ToDictionaryAsync(item => item.ReceiptItemId, item  => item.Quantity,cancellationToken);
        
        var stockQuantities = new Dictionary<long, Decimal>();
        foreach (var item in items)
        {
            var source = item.GoodReceiptItem;

            if (item.CompanyId != companyId || source == null || source.DeletedOn.HasValue || source.CompanyId != companyId)
            {
                throw new InvalidOperationException("The source receipt item is unavailable.");
            }

            var receipt = source.GoodReceipt;

            if (receipt.CompanyId != companyId|| receipt.DeletedOn.HasValue|| !receipt.Status)
            {
                throw new InvalidOperationException(
                    "Only active, received goods receipts can be returned.");
            }

            if (receipt.SupplierId != postedReturn.SupplierId)
            {
                throw new InvalidOperationException(
                    "The receipt supplier must match the return supplier.");
            }

            returnedQuantities.TryGetValue(source.Id, out var alreadyReturned);

            var returnableQuantity =
                source.ReceivedQuantity - alreadyReturned;

            if (item.ReturnQuantity <= 0
                || item.ReturnQuantity > returnableQuantity)    
            {
                throw new InvalidOperationException(
                    $"Invalid return quantity for receipt item {source.Id}. " +
                    $"Remaining returnable quantity is {returnableQuantity}.");
            }

            if (source.ReceivedQuantity <= 0|| !source.ReceivedBaseQuantity.HasValue|| source.ReceivedBaseQuantity.Value <= 0)
            {
                throw new InvalidOperationException(
                    "The receipt has no valid recorded stock quantity.");
            }

            var totalBaseReturned = decimal.Round((alreadyReturned + item.ReturnQuantity) * source.ReceivedBaseQuantity.Value / source.ReceivedQuantity,4);

            var previousBaseReturned = decimal.Round(alreadyReturned * source.ReceivedBaseQuantity.Value / source.ReceivedQuantity,4);

            var stockQuantity = totalBaseReturned - previousBaseReturned;

            if (stockQuantity <= 0)
            {
                throw new InvalidOperationException(
                    "Return quantity is below the supported stock precision.");
            }

            stockQuantities.Add(item.Id, stockQuantity);
        }
        var stockGroups = items.GroupBy(item => new
        {
            ProductId = item.GoodReceiptItem!.ProductId,
            WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId
        });

        foreach (var group in stockGroups
            .OrderBy(group => group.Key.ProductId)
            .ThenBy(group => group.Key.WarehouseId))
        {
            var requiredQuantity = group.Sum(item => stockQuantities[item.Id]);

            if (!await _context.Warehouses.AnyAsync(w => w.Id == group.Key.WarehouseId && w.CompanyId == companyId
                && !w.DeletedOn.HasValue, cancellationToken))
                throw new InvalidOperationException("The source warehouse is unavailable.");

            var stock = await _context.WarehouseStocks
                .SingleOrDefaultAsync(
                    stock => stock.CompanyId == companyId
                        && stock.ProductId == group.Key.ProductId
                        && stock.WarehouseId == group.Key.WarehouseId
                        && !stock.DeletedOn.HasValue,
                    cancellationToken);

            if (stock == null || stock.Quantity < requiredQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product {group.Key.ProductId} " +
                    $"in warehouse {group.Key.WarehouseId}. " +
                    $"Required quantity is {requiredQuantity}.");
            }

            stock.Quantity -= requiredQuantity;
        }

        var now = DateTime.Now;
        foreach (var item in items)
        {
            var source = item.GoodReceiptItem!;
            _context.StockTransactions.Add(new StockTransaction
            {
                CompanyId = companyId,
                ProductId = source.ProductId,
                WarehouseId = source.GoodReceipt.WarehouseId,
                WarehouseLocationId = 0,
                Quantity = stockQuantities[item.Id],
                TransactionType = "OUT",
                TransactionDate = now,
                UserId = userId,
                PurchaseReturnId = postedReturn.Id,
                PurchaseReturnItemId = item.Id,
                Note = $"Purchase Return: {postedReturn.ReturnNo}"
            });
        }

        var productIds = items.Select(item => item.GoodReceiptItem!.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => p.CompanyId == companyId && productIds.Contains(p.Id) && !p.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more return products are unavailable.");

        postedReturn.Status = PurchaseReturnStatus.Posted;
        postedReturn.PostedBy = userId;
        postedReturn.PostedOn = now;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CurrentStock = await _context.WarehouseStocks
                .Where(stock => stock.CompanyId == companyId && stock.ProductId == product.Id && !stock.DeletedOn.HasValue)
                .SumAsync(stock => stock.Quantity, cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);

        var response = await _sender.Send(new GetPurchaseReturnByIdQuery(postedReturn.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}

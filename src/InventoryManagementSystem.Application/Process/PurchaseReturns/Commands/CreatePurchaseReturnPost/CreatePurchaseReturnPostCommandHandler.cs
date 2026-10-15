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
        }

        throw new NotImplementedException("Purchase return posting is not implemented yet.");
    }
}

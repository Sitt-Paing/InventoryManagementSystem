using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.UpdatePurchaseReturnDraft;

public class UpdatePurchaseReturnCommandHandler : IRequestHandler<UpdatePurchaseReturnCommand, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public UpdatePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<PurchaseReturnDto> Handle(UpdatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var purchaseReturn = await _context.PurchaseReturns
            .Include(pr => pr.Items)
            .SingleOrDefaultAsync(pr => pr.Id == request.Id && pr.CompanyId == companyId
                && !pr.DeletedOn.HasValue, cancellationToken);
        if (purchaseReturn == null)
            throw new KeyNotFoundException("Purchase Return was not found.");
        if (purchaseReturn.Status != PurchaseReturnStatus.Draft)
            throw new InvalidOperationException("Only draft purchase returns can be updated.");

        bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId && !s.DeletedOn.HasValue, cancellationToken);
        if(supplierExists == false)
        {
            throw new InvalidOperationException($"Supplier with ID {request.SupplierId} does not exist.");
        }

        List<long> receiptItemIds = request.Items.Select(i => i.GoodReceiptItemId).ToList();

        List<GoodReceiptItem> receiptItems = await _context.GoodReceiptItems
            .AsNoTracking()
            .Include(item => item.GoodReceipt)
            .Where(item => receiptItemIds.Contains(item.Id) && item.CompanyId == companyId && !item.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        if(receiptItems.Count != receiptItemIds.Count)
        {
            throw new InvalidOperationException("One or more Good Receipt Items do not exist.");
        }

        Dictionary<long, decimal> returnedQuantities = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId
            && receiptItemIds.Contains(i.GoodReceiptItemId)
            && !i.DeletedOn.HasValue
            && i.PurchaseReturn!.CompanyId == companyId
            && !i.PurchaseReturn.DeletedOn.HasValue
            && i.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
            .GroupBy(i => i.GoodReceiptItemId)
             .Select(group => new
             {
                 ReceiptItemId = group.Key,
                 Quantity = group.Sum(item => item.ReturnQuantity)
             }).ToDictionaryAsync(item => item.ReceiptItemId,item => item.Quantity,cancellationToken);

        foreach (var receiptItem in receiptItems)
        {
            var receipt = receiptItem.GoodReceipt;

            if (receipt.CompanyId != companyId
                || receipt.DeletedOn.HasValue
                || !receipt.Status)
            {
                throw new InvalidOperationException(
                    "Only active, received goods receipts can be returned.");
            }

            if (receipt.SupplierId != request.SupplierId)
            {
                throw new InvalidOperationException(
                    "All receipt items must belong to the selected supplier.");
            }

            returnedQuantities.TryGetValue(receiptItem.Id, out var alreadyReturned);

            decimal returnableQuantity = receiptItem.ReceivedQuantity - alreadyReturned;

            CreatePurchaseReturnItemInput requestedItem = request.Items.Single(item => item.GoodReceiptItemId == receiptItem.Id);

            if (requestedItem.ReturnQuantity > returnableQuantity)
            {
                throw new InvalidOperationException(
                    $"Return quantity exceeds the remaining quantity for receipt item {receiptItem.Id}.");
            }
        }

        purchaseReturn.SupplierId = request.SupplierId;
        purchaseReturn.ReturnDate = request.ReturnDate;
        purchaseReturn.ReturnReason = request.ReturnReason?.Trim();

        // Reuse existing rows, including removed lines, to keep the receipt-item unique index valid.
        foreach (var item in purchaseReturn.Items.Where(i => !i.DeletedOn.HasValue
            && !receiptItemIds.Contains(i.GoodReceiptItemId)))
        {
            _context.PurchaseReturnItems.Remove(item);
        }
        foreach (var input in request.Items)
        {
            var item = purchaseReturn.Items.SingleOrDefault(i => i.GoodReceiptItemId == input.GoodReceiptItemId);
            if (item == null)
            {
                item = new PurchaseReturnItem { GoodReceiptItemId = input.GoodReceiptItemId, CompanyId = companyId };
                purchaseReturn.Items.Add(item);
            }
            item.ReturnQuantity = input.ReturnQuantity;
            item.DeletedOn = null;
            item.DeletedBy = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        var response = await _sender.Send(new GetPurchaseReturnByIdQuery(purchaseReturn.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}

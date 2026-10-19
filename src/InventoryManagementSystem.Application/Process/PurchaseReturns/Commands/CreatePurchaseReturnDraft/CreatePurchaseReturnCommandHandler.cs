using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, PurchaseReturnDto> 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;
    public CreatePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        string requestHash = CalculateRequestHash(request);
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);


        PurchaseReturn? existingReturn = await _context.PurchaseReturns
            .AsNoTracking()
            .SingleOrDefaultAsync(pr => pr.IdempotencyKey == request.IdempotencyKey && pr.CompanyId == companyId, cancellationToken);

        if (existingReturn != null)
        {
            if (existingReturn.DeletedOn.HasValue)
            {
                throw new InvalidOperationException(
                    "The return created with this key has been deleted.");
            }

            if (!string.Equals(
                existingReturn.IdempotencyRequestHash,
                requestHash,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "This idempotency key was already used for a different request.");
            }

            var existingResponse = await _sender.Send(new GetPurchaseReturnByIdQuery(existingReturn.Id), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existingResponse;
        }

        var returnNo = request.ReturnNo.Trim();
        if (await _context.PurchaseReturns.AnyAsync(pr => pr.CompanyId == companyId
            && pr.ReturnNo == returnNo, cancellationToken))
            throw new InvalidOperationException("Return number is already in use. Reopen Create Return to get a new number.");

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

        PurchaseReturn purchaseReturn = new PurchaseReturn
        {
            ReturnNo = returnNo,
            SupplierId = request.SupplierId,
            ReturnDate = request.ReturnDate,
            ReturnReason = request.ReturnReason?.Trim(),
            IdempotencyKey = request.IdempotencyKey,
            IdempotencyRequestHash = requestHash,
            Status = PurchaseReturnStatus.Draft,
            CompanyId = companyId
        };
        foreach (var item in request.Items)
        {   
            purchaseReturn.Items.Add(new PurchaseReturnItem
            {
                GoodReceiptItemId = item.GoodReceiptItemId,
                ReturnQuantity = item.ReturnQuantity,
                CompanyId = companyId
            });
        }
        _context.PurchaseReturns.Add(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
        var response = await _sender.Send(new GetPurchaseReturnByIdQuery(purchaseReturn.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    private static string CalculateRequestHash(CreatePurchaseReturnCommand command)
    {
        var payload = new
        {
            ReturnNo = command.ReturnNo.Trim(),
            command.SupplierId,
            ReturnDate = command.ReturnDate.ToString("O",CultureInfo.InvariantCulture),
            ReturnReason = command.ReturnReason?.Trim(),
            Items = command.Items.OrderBy(i => i.GoodReceiptItemId).Select(i => new { i.GoodReceiptItemId,
                ReturnQuantity = i.ReturnQuantity.ToString(
                    "G29", CultureInfo.InvariantCulture)
            }).ToArray()
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));
    }
}

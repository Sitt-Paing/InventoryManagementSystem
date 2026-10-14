using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands;

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, PurchaseReturnDto> 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    public CreatePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        string requestHash = CalculateRequestHash(request);
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

            return await GetResponseAsync(existingReturn.Id, companyId, cancellationToken);
        }
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
            ReturnNo = request.ReturnNo.Trim(),
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
        return await GetResponseAsync(purchaseReturn.Id, companyId, cancellationToken);
    }

    private async Task<PurchaseReturnDto> GetResponseAsync(
        Guid returnId, int? companyId, CancellationToken cancellationToken)
    {
        var result = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(pr => pr.Id == returnId && pr.CompanyId == companyId && !pr.DeletedOn.HasValue)
            .Select(pr => new PurchaseReturnDto
        {
            Id = pr.Id,
            ReturnNo = pr.ReturnNo,
            SupplierId = pr.SupplierId,
            SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
            ReturnDate = pr.ReturnDate,
            ReturnReason = pr.ReturnReason,
            Status = pr.Status,
            PostedBy = pr.PostedBy,
            PostedOn = pr.PostedOn,
            CreatedBy = pr.CreatedBy,
            CreatedOn = pr.CreatedOn,
            Items = pr.Items
                .Where(item => item.CompanyId == companyId && !item.DeletedOn.HasValue)
                .OrderBy(item => item.Id)
                .Select(item => new PurchaseReturnItemDto
            {
                Id = item.Id,
                PurchaseReturnId = item.PurchaseReturnId,
                GoodReceiptItemId = item.GoodReceiptItemId,
                GoodReceiptId = item.GoodReceiptItem!.GoodReceiptId,
                ReceiptNo = item.GoodReceiptItem.GoodReceipt.ReceiptNo,
                PurchaseOrderId = item.GoodReceiptItem.GoodReceipt.PurchaseOrderId,
                PurchaseOrderNo = item.GoodReceiptItem.GoodReceipt.PurchaseOrder != null
                    ? item.GoodReceiptItem.GoodReceipt.PurchaseOrder.PurchaseOrderNo : string.Empty,
                ProductId = item.GoodReceiptItem.ProductId,
                ProductName = item.GoodReceiptItem.Product.Name,
                UomId = item.GoodReceiptItem.UomId,
                UomName = item.GoodReceiptItem.Uom.Name,
                WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId,
                WarehouseName = item.GoodReceiptItem.GoodReceipt.Warehouse != null
                    ? item.GoodReceiptItem.GoodReceipt.Warehouse.Name : string.Empty,
                ReturnQuantity = item.ReturnQuantity
            }).ToList()
        }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new KeyNotFoundException("Purchase Return was not found.");

        if (!string.IsNullOrWhiteSpace(result.PostedBy))
        {
            var names = await _identityService.GetUserNamesAsync(new[] { result.PostedBy }, cancellationToken);
            result.PostedByName = names.TryGetValue(result.PostedBy, out var name) ? name : "Unknown user";
        }

        return result;
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

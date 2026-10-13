using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands;

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, PurchaseReturnDto> 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    public CreatePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        var userName = _currentUserService.UserName;
        var existingReturn = await _context.PurchaseReturns
            .FirstOrDefaultAsync(pr => pr.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingReturn != null)
        {
            return new PurchaseReturnDto
            {
                Id = existingReturn.Id,
                ReturnNo = existingReturn.ReturnNo,
                SupplierId = existingReturn.SupplierId,
                ReturnDate = existingReturn.ReturnDate,
                ReturnReason = existingReturn.ReturnReason,
                Status = existingReturn.Status,
                PostedBy = existingReturn.PostedBy,
                PostedOn = existingReturn.PostedOn,
                Items = _context.PurchaseReturnItems.Where(item => item.PurchaseReturnId == existingReturn.Id).Select(item => new PurchaseReturnItemDto
                {
                    Id = item.Id,
                    GoodReceiptItemId = item.GoodReceiptItemId,
                    ReturnQuantity = item.ReturnQuantity
                }).ToList()
            };
        }
        var purchaseReturn = new PurchaseReturn
        {
            ReturnNo = request.ReturnNo,
            SupplierId = request.SupplierId,
            ReturnDate = request.ReturnDate,
            ReturnReason = request.ReturnReason,
            IdempotencyKey = request.IdempotencyKey,
            Status = PurchaseReturnStatus.Draft,
        };
        foreach (var item in request.Items)
        {   
            purchaseReturn.Items.Add(new PurchaseReturnItem
            {
                GoodReceiptItemId = item.GoodReceiptItemId,
                ReturnQuantity = item.ReturnQuantity
            });
        }
        _context.PurchaseReturns.Add(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
        return new PurchaseReturnDto
        {
            Id = purchaseReturn.Id,
            ReturnNo = purchaseReturn.ReturnNo,
            SupplierId = purchaseReturn.SupplierId,
            ReturnDate = purchaseReturn.ReturnDate,
            ReturnReason = purchaseReturn.ReturnReason,
            Status = purchaseReturn.Status,
            Items = _context.PurchaseReturnItems.Where(item => item.PurchaseReturnId == purchaseReturn.Id).Select(item => new PurchaseReturnItemDto
            {
                Id = item.Id,
                GoodReceiptItemId = item.GoodReceiptItemId,
                ReturnQuantity = item.ReturnQuantity
            }).ToList()
        };
    }
}

using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
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
        PurchaseReturn? postedReturn = await _context.PurchaseReturns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PurchaseReturnId && x.IdempotencyKey == request.IdempotencyKey && !x.DeletedOn.HasValue, cancellationToken);
    }
}

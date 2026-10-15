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

        if(postedReturn.Status == PurchaseReturnStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft Return can be posted.");
        }

        throw new NotImplementedException("Purchase return posting is not implemented yet.");
    }
}

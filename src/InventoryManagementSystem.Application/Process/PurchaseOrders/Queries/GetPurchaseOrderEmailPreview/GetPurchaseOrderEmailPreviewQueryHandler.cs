using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailPreview;

public class GetPurchaseOrderEmailPreviewQueryHandler : IRequestHandler<GetPurchaseOrderEmailPreviewQuery, PurchaseOrderEmailPreviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrderEmailPreviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderEmailPreviewDto> Handle(GetPurchaseOrderEmailPreviewQuery request, CancellationToken cancellationToken)
    {
        var existingPo = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == request.PoId, cancellationToken);

        if (existingPo == null) throw new KeyNotFoundException($"There is no purchase order with Id {request.PoId}");
    }
}

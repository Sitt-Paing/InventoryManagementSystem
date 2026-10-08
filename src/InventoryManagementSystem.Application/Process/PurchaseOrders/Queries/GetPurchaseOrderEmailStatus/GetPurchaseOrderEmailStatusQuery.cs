using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailStatus;

public record GetPurchaseOrderEmailStatusQuery(Guid PoId, Guid EmailId) : IRequest<PurchaseOrderEmailStatusDto>;

public record PurchaseOrderEmailStatusDto(Guid Id, string Status, int Attempts, DateTime? SentOn, DateTime? NextAttemptOn);

public class GetPurchaseOrderEmailStatusQueryHandler : IRequestHandler<GetPurchaseOrderEmailStatusQuery, PurchaseOrderEmailStatusDto>
{
    private readonly IApplicationDbContext _context;

    public GetPurchaseOrderEmailStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrderEmailStatusDto> Handle(GetPurchaseOrderEmailStatusQuery request, CancellationToken cancellationToken)
    {
        var orderExists = await _context.PurchaseOrders.AnyAsync(
            x => x.Id == request.PoId && !x.DeletedOn.HasValue, cancellationToken);
        if (!orderExists) throw new KeyNotFoundException("Purchase order not found.");

        var email = await _context.EmailOutboxes.AsNoTracking().FirstOrDefaultAsync(
            x => x.Id == request.EmailId && x.PurchaseOrderId == request.PoId && !x.DeletedOn.HasValue, cancellationToken)
            ?? throw new KeyNotFoundException("Email request not found.");

        return new PurchaseOrderEmailStatusDto(email.Id, email.Status.ToString(), email.Attempts, email.SentOn, email.NextAttemptOn);
    }
}

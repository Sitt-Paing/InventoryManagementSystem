using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetLastPurchaseReturnNumber;

public class GetLastPurchaseReturnNumberQueryHandler : IRequestHandler<GetLastPurchaseReturnNumberQuery, string?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLastPurchaseReturnNumberQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public Task<string?> Handle(GetLastPurchaseReturnNumberQuery request, CancellationToken cancellationToken)
    {
        var prefix = $"PR-{request.Year}-";
        // Include deleted returns so their document numbers are never reused.
        return _context.PurchaseReturns.AsNoTracking()
            .Where(pr => pr.CompanyId == _currentUserService.CompanyId && pr.ReturnNo.StartsWith(prefix))
            .OrderByDescending(pr => pr.ReturnNo.Length)
            .ThenByDescending(pr => pr.ReturnNo)
            .Select(pr => pr.ReturnNo)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

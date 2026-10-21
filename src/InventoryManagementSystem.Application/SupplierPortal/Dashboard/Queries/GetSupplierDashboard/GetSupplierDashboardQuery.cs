using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.SupplierPortal.DTOs;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.SupplierPortal.Dashboard.Queries.GetSupplierDashboard;

public record GetSupplierDashboardQuery : IRequest<SupplierDashboardDto>
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public class GetSupplierDashboardQueryHandler : IRequestHandler<GetSupplierDashboardQuery, SupplierDashboardDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetSupplierDashboardQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<SupplierDashboardDto> Handle(GetSupplierDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.CompanyId.HasValue || !_currentUser.SupplierId.HasValue)
            throw new UnauthorizedAccessException("A supplier account is required.");
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value.Date > request.EndDate.Value.Date)
            throw new ArgumentException("Start date must not be after end date.");

        var supplierId = _currentUser.SupplierId.Value;
        var orders = _context.PurchaseOrders.AsNoTracking().Where(po => po.SupplierId == supplierId
            && !po.DeletedOn.HasValue && po.ApprovalStatus == PurchaseOrderApprovalStatus.Approved
            && po.Status != PurchaseOrderStatus.Cancelled);
        var receipts = _context.GoodReceipts.AsNoTracking().Where(gr => gr.SupplierId == supplierId
            && !gr.DeletedOn.HasValue && gr.Status);
        var returns = _context.PurchaseReturns.AsNoTracking().Where(pr => pr.SupplierId == supplierId
            && !pr.DeletedOn.HasValue && pr.Status == PurchaseReturnStatus.Posted);

        if (request.StartDate.HasValue)
        {
            orders = orders.Where(po => po.OrderDate.Date >= request.StartDate.Value.Date);
            receipts = receipts.Where(gr => gr.ReceiptDate.Date >= request.StartDate.Value.Date);
            returns = returns.Where(pr => pr.ReturnDate.Date >= request.StartDate.Value.Date);
        }
        if (request.EndDate.HasValue)
        {
            orders = orders.Where(po => po.OrderDate.Date <= request.EndDate.Value.Date);
            receipts = receipts.Where(gr => gr.ReceiptDate.Date <= request.EndDate.Value.Date);
            returns = returns.Where(pr => pr.ReturnDate.Date <= request.EndDate.Value.Date);
        }

        var orderSummary = await orders.GroupBy(po => 1)
            .Select(group => new SupplierDocumentSummaryDto { Count = group.Count(), TotalAmount = group.Sum(po => po.TotalAmount) })
            .FirstOrDefaultAsync(cancellationToken) ?? new SupplierDocumentSummaryDto();
        var receiptSummary = await receipts.GroupBy(gr => 1)
            .Select(group => new SupplierDocumentSummaryDto { Count = group.Count(), TotalAmount = group.Sum(gr => gr.TotalAmount) })
            .FirstOrDefaultAsync(cancellationToken) ?? new SupplierDocumentSummaryDto();
        var returnSummary = await returns.GroupBy(pr => 1)
            .Select(group => new SupplierDocumentSummaryDto { Count = group.Count(), TotalAmount = group.Sum(pr => pr.TotalAmount) })
            .FirstOrDefaultAsync(cancellationToken) ?? new SupplierDocumentSummaryDto();

        return new SupplierDashboardDto
        {
            PurchaseOrders = orderSummary,
            GoodsReceipts = receiptSummary,
            PurchaseReturns = returnSummary,
            NetReceivedAmount = receiptSummary.TotalAmount - returnSummary.TotalAmount
        };
    }
}

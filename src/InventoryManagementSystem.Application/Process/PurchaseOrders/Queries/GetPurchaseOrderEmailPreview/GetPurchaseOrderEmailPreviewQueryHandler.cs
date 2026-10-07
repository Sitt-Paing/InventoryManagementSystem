using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
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

        var supplier = await _context.Suppliers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == existingPo.SupplierId && !x.DeletedOn.HasValue, cancellationToken);

        if (supplier == null) throw new InvalidOperationException("The purchase order's supplier is unavailable.");

        var email = supplier.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("An email address is required for this supplier.");
        if (!MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            throw new InvalidOperationException("The supplier's email address is invalid.");
        }

        var items = await _context.PurchaseOrderItems
            .AsNoTracking()
            .Where(x => x.PurchaseOrderId == request.PoId && !x.DeletedOn.HasValue)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                ProductName = x.Product != null ? x.Product.Name : null,
                x.Quantity,
                UomName = x.Uom != null ? x.Uom.Name : null,
                x.UnitPrice
            })
            .ToListAsync(cancellationToken);

        if (items.Count == 0) throw new InvalidOperationException("The purchase order has no active items to preview.");

        var culture = CultureInfo.InvariantCulture;
        const string numberFormat = "#,##0.############################";
        var body = new StringBuilder();
        body.AppendLine($"Purchase Order: {existingPo.PurchaseOrderNo}");
        body.AppendLine($"Supplier: {supplier.CompanyName}");
        body.AppendLine($"Order Date: {existingPo.OrderDate.ToString("dd/MMM/yyyy", culture)}");
        body.AppendLine($"Expected Date: {existingPo.ExpectedDate.ToString("dd/MMM/yyyy", culture)}");
        body.AppendLine();

        decimal total = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductName) || string.IsNullOrWhiteSpace(item.UomName))
            {
                throw new InvalidOperationException("A purchase order item has unavailable product or UoM details.");
            }

            var amount = item.Quantity * item.UnitPrice;
            total += amount;
            body.AppendLine($"{item.ProductName} | {item.Quantity.ToString(numberFormat, culture)} {item.UomName} | Unit Price: {item.UnitPrice.ToString(numberFormat, culture)} | Amount: {amount.ToString(numberFormat, culture)}");
        }

        body.AppendLine();
        body.AppendLine($"Total: {total.ToString(numberFormat, culture)}");

        return new PurchaseOrderEmailPreviewDto
        {
            To = email,
            Subject = $"Purchase Order — {existingPo.PurchaseOrderNo}",
            Body = body.ToString()
        };
    }
}

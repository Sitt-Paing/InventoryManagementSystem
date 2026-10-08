using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailPreview;
using MediatR;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SendPurchaseOrderEmail;

public class SendPurchaseOrderEmailCommandHandler : IRequestHandler<SendPurchaseOrderEmailCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    public SendPurchaseOrderEmailCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }
    public async Task<Guid> Handle(SendPurchaseOrderEmailCommand command, CancellationToken cancellationToken)
    {
        if (command.IdempotencyKey == Guid.Empty)
            throw new ArgumentException("A non-empty Idempotency-Key is required.");

        using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        var order = await _context.PurchaseOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.PoId && !x.DeletedOn.HasValue, cancellationToken)
            ?? throw new KeyNotFoundException("Purchase order not found.");

        var existing = await _context.EmailOutboxes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.IdempotencyKey, cancellationToken);
        if (existing != null)
        {
            if (existing.PurchaseOrderId != command.PoId || existing.CompanyId != order.CompanyId)
                throw new InvalidOperationException("This Idempotency-Key belongs to another email request.");
            await transaction.CommitAsync(cancellationToken);
            return existing.Id;
        }

        var emailPreview = await _mediator.Send(new GetPurchaseOrderEmailPreviewQuery(command.PoId), cancellationToken);
        var email = new EmailOutbox
        {
            Id = command.IdempotencyKey,
            CompanyId = order.CompanyId,
            PurchaseOrderId = command.PoId,
            To = emailPreview.To,
            Subject = emailPreview.Subject,
            Body = emailPreview.Body
        };
        _context.EmailOutboxes.Add(email);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return email.Id;
    }
}

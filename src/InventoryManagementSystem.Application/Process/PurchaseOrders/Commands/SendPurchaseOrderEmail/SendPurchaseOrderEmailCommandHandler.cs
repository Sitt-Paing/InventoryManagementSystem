using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailPreview;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SendPurchaseOrderEmail;

public class SendPurchaseOrderEmailCommandHandler : IRequestHandler<SendPurchaseOrderEmailCommand>
{
    private readonly IEmailService _emailService;
    private readonly IMediator _mediator;
    public SendPurchaseOrderEmailCommandHandler(IEmailService emailService, IMediator mediator)
    {
        _emailService = emailService;
        _mediator = mediator;
    }
    public async Task Handle(SendPurchaseOrderEmailCommand command, CancellationToken cancellationToken)
    {
        var emailPreview = await _mediator.Send(new GetPurchaseOrderEmailPreviewQuery(command.PoId), cancellationToken);
        await _emailService.SendAsync(emailPreview.To, emailPreview.Subject, emailPreview.Body, cancellationToken);
        return;
    }
}

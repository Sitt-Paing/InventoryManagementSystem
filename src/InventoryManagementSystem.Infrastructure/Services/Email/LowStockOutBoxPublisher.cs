using DocumentFormat.OpenXml.Spreadsheet;
using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public sealed class LowStockOutBoxPublisher
{
    public const string QueueName = "inventory.low-stock-notifications";

    public async Task PublishPendingAsync(InventoryManagementDbContext context, IChannel channel, CancellationToken cancellationToken)
    {
        var pending = await context.LowStockOutboxes
                        .IgnoreQueryFilters()
                        .Where(row => row.PublishedOn == null && row.DeletedOn == null) 
                        .OrderBy(row => row.CreatedOn)
                        .ThenBy(row => row.Id)
                        .Take(50)
                        .ToListAsync(cancellationToken);

        foreach(var row in pending)
        {
            var messageId = row.Id.ToString("D");

            var properties = new BasicProperties
            {
                MessageId = messageId,
                ContentType = "text/plain",
                Persistent = true
            };

            await channel.BasicPublishAsync(
              exchange: "",
              routingKey: QueueName,
              mandatory: true,
              basicProperties: properties,
              body: Encoding.UTF8.GetBytes(messageId),
              cancellationToken: cancellationToken);

            row.MarkPublished(DateTime.Now);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

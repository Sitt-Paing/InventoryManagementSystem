using DocumentFormat.OpenXml.Wordprocessing;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public sealed class LowStockNotificationWriter
{
    private readonly InventoryManagementDbContext _context;
    private readonly DbContextOptions<InventoryManagementDbContext> _options;

    public LowStockNotificationWriter(
        InventoryManagementDbContext context,
        DbContextOptions<InventoryManagementDbContext> options)
    {
        _context = context;
        _options = options;
    }

    public async Task<bool> CreateAsync(Guid outBoxId, CancellationToken cancellationToken)
    {
        var existing = await _context.LowStockOutboxes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == outBoxId && !row.DeletedOn.HasValue, cancellationToken);
        if (existing == null)
        {
            return false;
        }

        var companyId = existing?.CompanyId ?? throw new InvalidOperationException("Outbox company is missing.");

        await using var companyContext = new InventoryManagementDbContext( _options, new NotificationWorkerContext(companyId));

        await using var transaction = await companyContext.BeginTransactionAsync(cancellationToken);

        bool alreadyExists = await companyContext.LowStockNotifications.AnyAsync(row => row.Id == existing.Id, cancellationToken);
        if (!alreadyExists)
        {
            companyContext.LowStockNotifications.Add(new LowStockNotification
            {
                Id = existing.Id,
                CompanyId = companyId,
                ProductId = existing.ProductId,
                CurrentQuantity = existing.CurrentQuantity,
                ReorderLevel = existing.ReorderLevel
            });

            await companyContext.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
    private sealed class NotificationWorkerContext(int companyId): ICurrentUserService
    {
        public int? CompanyId => companyId;
        public string? UserId => null;
        public string? UserName => "LowStockWorker";
        public string? IpAddress => null;
        public int? SupplierId => null;
        public bool IsSuperAdmin => false;
    }
}
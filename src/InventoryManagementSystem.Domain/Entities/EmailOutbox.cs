using InventoryManagementSystem.Domain.Common;

namespace InventoryManagementSystem.Domain.Entities;

public class EmailOutbox : BaseAuditableEntity<Guid>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailOutboxStatus Status { get; private set; } = EmailOutboxStatus.Pending;
    public int Attempts { get; private set; }
    public DateTime? NextAttemptOn { get; private set; }
    public DateTime? SentOn { get; private set; }
    public string? LastError { get; private set; }

    public void StartSending(DateTime utcNow)
    {
        if (Status != EmailOutboxStatus.Pending)
            throw new InvalidOperationException("Only pending emails can be sent.");
        if (NextAttemptOn.HasValue && NextAttemptOn.Value > utcNow)
            throw new InvalidOperationException("The email is not due for retry yet.");

        Status = EmailOutboxStatus.Sending;
        Attempts++;
        NextAttemptOn = null;
    }

    public void MarkSent(DateTime utcNow)
    {
        if (Status != EmailOutboxStatus.Sending)
            throw new InvalidOperationException("Only sending emails can be marked as sent.");

        Status = EmailOutboxStatus.Sent;
        SentOn = utcNow;
        NextAttemptOn = null;
        LastError = null;
    }

    public void MarkFailed(string error, DateTime? nextAttemptOn = null)
    {
        if (Status != EmailOutboxStatus.Sending)
            throw new InvalidOperationException("Only sending emails can be marked as failed.");
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("An error message is required.", nameof(error));

        LastError = error;
        NextAttemptOn = nextAttemptOn;
        Status = nextAttemptOn.HasValue ? EmailOutboxStatus.Pending : EmailOutboxStatus.Failed;
    }
}

public enum EmailOutboxStatus
{
    Pending,
    Sending,
    Sent,
    Failed
}

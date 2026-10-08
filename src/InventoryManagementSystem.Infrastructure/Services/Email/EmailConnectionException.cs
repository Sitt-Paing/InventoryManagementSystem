namespace InventoryManagementSystem.Infrastructure.Services.Email;

public class EmailConnectionException : Exception
{
    public EmailConnectionException(Exception innerException)
        : base("Could not connect to the SMTP server before sending.", innerException)
    {
    }
}

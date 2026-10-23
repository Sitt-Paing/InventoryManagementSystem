namespace InventoryManagementSystem.Application.Dashboard.Exceptions;

public sealed class DashboardGenerationException : Exception
{
    public DashboardGenerationException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
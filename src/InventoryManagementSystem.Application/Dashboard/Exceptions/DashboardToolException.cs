namespace InventoryManagementSystem.Application.Dashboard.Exceptions;

public sealed class DashboardToolException(string message, Exception innerException)
    : Exception(message, innerException);

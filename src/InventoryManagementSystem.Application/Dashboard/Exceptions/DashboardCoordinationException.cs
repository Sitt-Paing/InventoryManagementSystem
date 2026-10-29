namespace InventoryManagementSystem.Application.Dashboard.Exceptions;

public sealed class DashboardCoordinationException(string message, Exception? innerException = null)
    : Exception(message, innerException);

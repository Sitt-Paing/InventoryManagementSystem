namespace InventoryManagementSystem.Application.Common.Models;

public class DefaultResponseModel
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

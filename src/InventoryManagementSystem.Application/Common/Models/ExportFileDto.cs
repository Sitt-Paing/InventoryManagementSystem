namespace InventoryManagementSystem.Application.Common.Models;

public class ExportFileDto
{
    public required byte[] Content { get; set; }
    public required string ContentType { get; set; }
    public required string FileName { get; set; }
}

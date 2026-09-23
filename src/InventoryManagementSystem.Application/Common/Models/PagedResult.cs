using System.Collections.Generic;

namespace InventoryManagementSystem.Application.Common.Models;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalRecords { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

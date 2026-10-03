using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.DTOs;

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
}


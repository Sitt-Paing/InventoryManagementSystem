using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    long CategoryId,
    string? Sku = null,
    decimal SellingPrice = 0,
    int CurrentStock = 0,
    int ReorderLevel = 0,
    string? Barcode = null,
    string? Brand = null,
    string? Unit = null,
    decimal CostPrice = 0,
    int ReorderQuantity = 0,
    decimal Tax = 0,
    bool Status = true,
    string? Description = null) : IRequest<ProductDto?>;

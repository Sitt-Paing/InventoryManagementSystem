using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    long CategoryId,
    long BaseUomId,
    string? Sku = null,
    decimal SellingPrice = 0,
    decimal CurrentStock = 0,
    decimal ReorderLevel = 0,
    string? Barcode = null,
    string? Brand = null,
    string? Unit = null,
    long? PurchaseUomId = null,
    long? SaleUomId = null,
    decimal CostPrice = 0,
    decimal ReorderQuantity = 0,
    decimal Tax = 0,
    bool Status = true,
    string? Description = null) : IRequest<ProductDto?>;

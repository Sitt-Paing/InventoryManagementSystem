using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    long CategoryId,
    decimal SellingPrice = 0,
    string? Sku = null,
    int CurrentStock = 0,
    int ReorderLevel = 0,
    string? Barcode = null,
    string? Brand = null,
    string? Unit = null,
    decimal CostPrice = 0,
    int ReorderQuantity = 0,
    decimal Tax = 0,
    bool Status = true,
    string? Description = null) : IRequest<ProductDto>;

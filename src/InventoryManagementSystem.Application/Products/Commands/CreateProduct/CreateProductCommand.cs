using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(string Name, long categoryId, decimal UnitPrice, string? Sku, int CurrentStock, int ReorderLevel, string? Barcode = null) : IRequest<ProductDto>;

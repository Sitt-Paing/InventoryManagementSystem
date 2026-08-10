using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(Guid Id,string Name, long categoryId, decimal UnitPrice, string? Sku, int CurrentStock, int ReorderLevel) : IRequest<ProductDto>;

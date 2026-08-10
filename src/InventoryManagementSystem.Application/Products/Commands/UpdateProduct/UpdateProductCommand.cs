using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(Guid Id,string Name,long CategoryId,string Sku,decimal UnitPrice, int CurrentStock, int ReorderLevel) : IRequest<ProductDto?>;

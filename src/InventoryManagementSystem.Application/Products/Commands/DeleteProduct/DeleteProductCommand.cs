using InventoryManagementSystem.Application.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<ProductDto?>;
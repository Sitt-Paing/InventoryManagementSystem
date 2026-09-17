using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest<ProductDto?>;
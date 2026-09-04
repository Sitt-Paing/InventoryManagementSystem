using InventoryManagementSystem.Application.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.DeleteSupplier;

public record DeleteSupplierCommand(int id) : IRequest<SupplierDto>;

using InventoryManagementSystem.Application.Master.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Suppliers.Commands.DeleteSupplier;

public record DeleteSupplierCommand(int id) : IRequest<SupplierDto>;

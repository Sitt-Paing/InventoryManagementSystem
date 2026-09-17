using InventoryManagementSystem.Application.Master.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Suppliers.Queries.GetSuppliers;

public record GetSuppliersQuery() : IRequest<List<SupplierDto>>;

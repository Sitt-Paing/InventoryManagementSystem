using InventoryManagementSystem.Application.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(int Id) : IRequest<SupplierDto?>;

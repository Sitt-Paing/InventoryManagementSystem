using InventoryManagementSystem.Application.Master.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(int Id) : IRequest<SupplierDto?>;

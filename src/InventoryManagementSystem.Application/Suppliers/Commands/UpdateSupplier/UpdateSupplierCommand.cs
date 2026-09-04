using InventoryManagementSystem.Application.Suppliers.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand(int id, string supplierCode, string companyName, string? contactPerson, string? phone, string? email, string? address, string paymentTerms, decimal? creditLimit, bool status) : IRequest<SupplierDto>;

using InventoryManagementSystem.Application.Master.Warehouses.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Warehouses.Commands.UpdateWarehouse;

public record class UpdateWarehouseCommand(int Id, string WarehouseCode, string Name, string? Address, string? ContactPerson, string? Phone, string? Email, decimal? Capacity, bool Status) : IRequest<WarehouseDto?>;

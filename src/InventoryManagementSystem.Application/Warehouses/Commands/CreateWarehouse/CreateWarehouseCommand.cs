using InventoryManagementSystem.Application.Warehouses.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Warehouses.Commands.CreateWarehouse;

public record class CreateWarehouseCommand(int Id, string WarehouseCode, string Name,string? Address, string? ContactPerson, string? Phone, string? Email,decimal? Capacity, bool Status) : IRequest<WarehouseDto>;
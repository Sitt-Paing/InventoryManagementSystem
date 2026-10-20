using InventoryManagementSystem.Application.Master.Departments.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.DeleteDepartment;

public record DeleteDepartmentCommand(int id) : IRequest<DepartmentDto?>;

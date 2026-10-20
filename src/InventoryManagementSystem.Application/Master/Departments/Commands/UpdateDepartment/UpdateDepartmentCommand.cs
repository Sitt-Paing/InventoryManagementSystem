using InventoryManagementSystem.Application.Master.Departments.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.UpdateDepartment;

public record UpdateDepartmentCommand(int id, string deptName, bool isActive) : IRequest<DepartmentDto?>;

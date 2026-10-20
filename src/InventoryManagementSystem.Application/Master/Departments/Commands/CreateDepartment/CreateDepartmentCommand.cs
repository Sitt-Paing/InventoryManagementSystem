using InventoryManagementSystem.Application.Master.Departments.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(string deptName, bool isActive) : IRequest<DepartmentDto>;

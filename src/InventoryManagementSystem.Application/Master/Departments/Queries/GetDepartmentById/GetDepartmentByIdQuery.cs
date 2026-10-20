using InventoryManagementSystem.Application.Master.Departments.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Departments.Queries.GetDepartmentById;

public record GetDepartmentByIdQuery(int Id) : IRequest<DepartmentDto?>;

using InventoryManagementSystem.Application.Master.Departments.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Departments.Queries.GetDepartments;

public record GetDepartmentsQuery() : IRequest<List<DepartmentDto>>;

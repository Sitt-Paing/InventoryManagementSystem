using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Departments.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.CreateDepartment;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateDepartmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        Department entity = new Department
        {
            DeptName = request.deptName.Trim(),
            IsActive = request.isActive,
            CompanyId = _currentUserService.CompanyId
        };
        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new DepartmentDto
        {
            Id = entity.Id,
            DeptName = entity.DeptName,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };
    }
}

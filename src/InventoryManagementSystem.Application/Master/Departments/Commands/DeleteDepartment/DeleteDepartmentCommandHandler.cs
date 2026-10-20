using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Departments.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Master.Departments.Commands.DeleteDepartment;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, DepartmentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteDepartmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DepartmentDto?> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        Department? entity = await _context.Departments
            .FirstOrDefaultAsync(x => x.Id == request.id && !x.DeletedOn.HasValue
                && x.CompanyId == _currentUserService.CompanyId, cancellationToken);
        if (entity == null) return null;

        _context.Departments.Remove(entity);
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

using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Departments.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Master.Departments.Queries.GetDepartmentById;

public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto?>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentDto?> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.DeletedOn.HasValue, cancellationToken);
        if (entity == null) return null;
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

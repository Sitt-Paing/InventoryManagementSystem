using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.DeleteUomCategory;

public class DeleteUomCategoryCommandHandler : IRequestHandler<DeleteUomCategoryCommand, UomCategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteUomCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UomCategoryDto?> Handle(DeleteUomCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UomCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id && !c.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        var dto = new UomCategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy,
            DeletedOn = entity.DeletedOn,
            DeletedBy = entity.DeletedBy
        };

        _context.UomCategories.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return dto;
    }
}

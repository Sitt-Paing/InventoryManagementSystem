using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.UpdateUomCategory;

public class UpdateUomCategoryCommandHandler : IRequestHandler<UpdateUomCategoryCommand, UomCategoryDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateUomCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UomCategoryDto?> Handle(UpdateUomCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.UomCategories
            .FirstOrDefaultAsync(c => c.Id == request.Id && !c.DeletedOn.HasValue, cancellationToken);

        if (entity == null) return null;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return new UomCategoryDto
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
    }
}

using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.UomCategories.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.UomCategories.Commands.CreateUomCategory;

public class CreateUomCategoryCommandHandler : IRequestHandler<CreateUomCategoryCommand, UomCategoryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateUomCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UomCategoryDto> Handle(CreateUomCategoryCommand request, CancellationToken cancellationToken)
    {
        var entity = new UomCategory
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
        };

        _context.UomCategories.Add(entity);
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

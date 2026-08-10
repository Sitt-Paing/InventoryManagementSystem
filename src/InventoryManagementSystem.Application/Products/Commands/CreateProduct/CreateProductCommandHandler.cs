using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Products.DTOs;
using InventoryManagementSystem.Domain.Entities;
using MediatR;


namespace InventoryManagementSystem.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CategoryId = request.categoryId,
            UnitPrice = request.UnitPrice,
            Sku = request.Sku,
            CurrentStock = request.CurrentStock,
            ReorderLevel = request.ReorderLevel
        };
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            UnitPrice = entity.UnitPrice,
            Sku = entity.Sku,
            CurrentStock = entity.CurrentStock,
            ReorderLevel = entity.ReorderLevel,
            CreatedOn = DateTime.Now,
            CreatedBy = entity.CreatedBy,
        };
    }
}

using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;


namespace InventoryManagementSystem.Application.Common.Services;

public class UomConversionService : IUomConversionService
{
    private readonly IApplicationDbContext _context;
    private readonly UomQuantityCalculator _calculator;
    public UomConversionService(IApplicationDbContext context, UomQuantityCalculator calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<decimal> ConvertToBaseUomAsync(Guid productId,long fromUomId, decimal quantity, CancellationToken cancellationToken)
    {
        var product = await _context.Products.AsNoTracking().Where(x => !x.DeletedOn.HasValue).FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if(product == null)
        {
            throw new KeyNotFoundException($"Product '{productId}' was not found.");
        }

        if(fromUomId == product.BaseUomId)
        {
            return _calculator.Convert(quantity, 1m);
        }

        var conversion = await _context.ProductUomConversions
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.FromUomId == fromUomId && x.ToUomId == product.BaseUomId && x.IsActive, cancellationToken);

        if(conversion == null)
        {
            throw new InvalidOperationException($"No active conversion found for product '{productId}' from UoM '{fromUomId}' to base UoM '{product.BaseUomId}'.");
        }

        return _calculator.Convert(quantity, conversion.ConversionFactor);
    }
}

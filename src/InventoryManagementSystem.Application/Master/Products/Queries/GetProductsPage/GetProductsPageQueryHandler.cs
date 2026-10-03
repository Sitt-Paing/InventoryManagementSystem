using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductsPage;

public class GetProductsPageQueryHandler : IRequestHandler<GetProductsPageQuery, PagedResult<ProductListItemDto>>
{
    private readonly IApplicationDbContext _context;
    public GetProductsPageQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsPageQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking().Where(x => !x.DeletedOn.HasValue);

        if(request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.Contains(search));
        }
        
        var totalRecords = await query.CountAsync(cancellationToken);
        
        var products = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                SellingPrice = p.SellingPrice,
            }).ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = products,
            TotalRecords = totalRecords,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

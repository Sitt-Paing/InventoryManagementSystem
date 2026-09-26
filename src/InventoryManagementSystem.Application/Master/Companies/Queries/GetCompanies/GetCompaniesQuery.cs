using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Companies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.Companies.Queries.GetCompanies;

public record GetCompaniesQuery : IRequest<List<CompanyDto>>;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, List<CompanyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCompaniesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Companies
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .OrderByDescending(x => x.CreatedOn)
            .Select(c => new CompanyDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                Email = c.Email,
                Address = c.Address,
                ContactPerson = c.ContactPerson,
                Phone = c.Phone,
                IsActive = c.IsActive,
                UserCount = c.AspNetUsers.Count,
                CreatedOn = c.CreatedOn,
                CreatedBy = c.CreatedBy,
                UpdatedOn = c.UpdatedOn,
                UpdatedBy = c.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }
}

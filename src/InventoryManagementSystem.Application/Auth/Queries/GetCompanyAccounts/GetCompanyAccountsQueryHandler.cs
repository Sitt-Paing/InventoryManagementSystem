using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Application.Auth.Queries.GetCompanyAccounts;

public class GetCompanyAccountsQueryHandler : IRequestHandler<GetCompanyAccountsQuery, List<CompanyAccountDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyAccountsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<CompanyAccountDto>> Handle(GetCompanyAccountsQuery request, CancellationToken cancellationToken)
    {
        return _context.Companies.AsNoTracking()
            .Where(company => company.IsActive && !company.DeletedOn.HasValue)
            .OrderBy(company => company.CompanyName)
            .Select(company => new CompanyAccountDto(company.Id, company.CompanyName))
            .ToListAsync(cancellationToken);
    }
}

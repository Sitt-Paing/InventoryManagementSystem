using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Companies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.Companies.Queries.GetCompanyById;

public record GetCompanyByIdQuery(int Id) : IRequest<CompanyDto?>;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCompanyByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (company == null) return null;

        return new CompanyDto
        {
            Id = company.Id,
            CompanyName = company.CompanyName,
            Email = company.Email,
            Address = company.Address,
            ContactPerson = company.ContactPerson,
            Phone = company.Phone,
            IsActive = company.IsActive,
            UserCount = await _context.Companies
                .Where(c => c.Id == company.Id)
                .SelectMany(c => c.AspNetUsers)
                .CountAsync(cancellationToken),
            CreatedOn = company.CreatedOn,
            CreatedBy = company.CreatedBy,
            UpdatedOn = company.UpdatedOn,
            UpdatedBy = company.UpdatedBy
        };
    }
}

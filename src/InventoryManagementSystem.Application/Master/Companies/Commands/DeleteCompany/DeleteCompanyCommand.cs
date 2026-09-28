using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Companies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.Companies.Commands.DeleteCompany;

public record DeleteCompanyCommand(int Id) : IRequest<CompanyDto?>;

public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, CompanyDto?>
{
    private readonly IApplicationDbContext _context;

    public DeleteCompanyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto?> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Companies
            .Where(c => !c.DeletedOn.HasValue)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null) return null;

        entity.IsActive = false;
        _context.Companies.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new CompanyDto
        {
            Id = entity.Id,
            CompanyName = entity.CompanyName,
            Email = entity.Email,
            Address = entity.Address,
            ContactPerson = entity.ContactPerson,
            Phone = entity.Phone,
            IsActive = entity.IsActive,
            CreatedOn = entity.CreatedOn,
            CreatedBy = entity.CreatedBy,
            UpdatedOn = entity.UpdatedOn,
            UpdatedBy = entity.UpdatedBy
        };
    }
}

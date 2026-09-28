using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Master.Companies.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Master.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand : IRequest<CompanyDto?>
{
    public int Id { get; init; }
    public string CompanyName { get; init; } = null!;
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, CompanyDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateCompanyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyDto?> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Companies
            .Where(c => !c.DeletedOn.HasValue)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (entity == null) return null;

        entity.CompanyName = request.CompanyName;
        entity.Email = request.Email ?? string.Empty;
        entity.Address = request.Address ?? string.Empty;
        entity.ContactPerson = request.ContactPerson ?? string.Empty;
        entity.Phone = request.Phone ?? string.Empty;
        entity.IsActive = request.IsActive;

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

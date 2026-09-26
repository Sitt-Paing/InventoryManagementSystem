using InventoryManagementSystem.Application.Auth.Models;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Application.Auth.Commands.RegisterCompanyWithAdmin;

public class RegisterCompanyWithAdminCommandHandler : IRequestHandler<RegisterCompanyWithAdminCommand, AuthResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public RegisterCompanyWithAdminCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<AuthResultDto> Handle(RegisterCompanyWithAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Create Company record
        var company = new Company
        {
            CompanyName = request.CompanyName.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            Email = request.CompanyEmail.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            IsActive = true
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Create Company Admin user with company claims and FK
        var authResult = await _identityService.RegisterCompanyAdminAsync(
            request.AdminUserName.Trim(),
            request.AdminEmail.Trim(),
            request.AdminPassword,
            company.Id,
            company.CompanyName,
            request.Role ?? "CompanyAdmin");

        if (!authResult.Succeeded)
        {
            // Rollback company creation if user creation fails
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync(cancellationToken);

            return authResult;
        }

        authResult.Message = $"Company '{company.CompanyName}' and admin user '{request.AdminUserName}' created successfully.";
        return authResult;
    }
}

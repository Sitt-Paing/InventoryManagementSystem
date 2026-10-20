using InventoryManagementSystem.Application.Auth.Models;
using MediatR;

namespace InventoryManagementSystem.Application.Auth.Commands.SelectCompany;

public record SelectCompanyCommand(int CompanyId, bool RememberMe = false) : IRequest<AuthResultDto>;

using MediatR;

namespace InventoryManagementSystem.Application.Auth.Queries.GetCompanyAccounts;

public record CompanyAccountDto(int Id, string CompanyName);
public record GetCompanyAccountsQuery() : IRequest<List<CompanyAccountDto>>;

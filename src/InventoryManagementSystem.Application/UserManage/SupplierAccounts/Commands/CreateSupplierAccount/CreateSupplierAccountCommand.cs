using FluentValidation;
using InventoryManagementSystem.Application.Auth.Models;
using MediatR;

namespace InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.CreateSupplierAccount;

public record CreateSupplierAccountCommand : IRequest<ResultDto>
{
    public int SupplierId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}


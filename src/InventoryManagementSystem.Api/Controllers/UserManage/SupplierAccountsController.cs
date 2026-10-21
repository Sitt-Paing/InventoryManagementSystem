using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.CreateSupplierAccount;
using InventoryManagementSystem.Application.UserManage.SupplierAccounts.Commands.DeleteSupplierAccount;
using InventoryManagementSystem.Application.UserManage.SupplierAccounts.Queries.GetSupplierAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.UserManage;

[Authorize(Policy = "InternalUser")]
[Authorize(Roles = "SuperAdmin,Administrator,CompanyAdmin")]
[Route("api/usermanage/supplier-accounts")]
[ApiController]
public class SupplierAccountsController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get login accounts for a supplier in the current company")]
    public async Task<IActionResult> GetSupplierAccounts([FromQuery] int supplierId, CancellationToken cancellationToken)
    {
        if (supplierId <= 0) return BadRequest();

        var result = await Mediator.Send(new GetSupplierAccountsQuery(supplierId), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Supplier accounts retrieved successfully.",
            Data = result
        });
    }

    [HttpDelete("{userId}")]
    [EndpointSummary("Delete a supplier login account in the current company")]
    public async Task<IActionResult> DeleteSupplierAccount(string userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteSupplierAccountCommand(userId), cancellationToken);
        var response = new DefaultResponseModel
        {
            StatusCode = result.Succeeded ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            Success = result.Succeeded,
            Message = result.Message ?? (result.Succeeded ? "Supplier account deleted." : "Unable to delete supplier account."),
            Data = result.Succeeded ? result : result.Errors
        };
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost]
    [EndpointSummary("Create a supplier account in the current company")]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(DefaultResponseModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSupplierAccount(
        [FromBody] CreateSupplierAccountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Failed to create supplier account.",
                Data = result.Errors
            });
        }

        return StatusCode(StatusCodes.Status201Created, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = result.Message,
            Data = result
        });
    }
}

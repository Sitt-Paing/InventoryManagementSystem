using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/purchase-returns")]
[ApiController]
public class PurchaseReturnsController : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a purchase return by ID in the current company")]
    public async Task<IActionResult> GetPurchaseReturnById(Guid id, CancellationToken cancellationToken)
    {
        PurchaseReturnDto result = await Mediator.Send(new GetPurchaseReturnByIdQuery(id), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase return retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create a purchase return draft")]
    public async Task<IActionResult> CreatePurchaseReturn(
        [FromBody] CreatePurchaseReturnCommand command, CancellationToken cancellationToken)
    {
        PurchaseReturnDto result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPurchaseReturnById), new { id = result.Id }, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Purchase return draft saved successfully.",
            Data = result
        });
    }
}

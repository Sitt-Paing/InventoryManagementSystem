using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetLastPurchaseReturnNumber;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.UpdatePurchaseReturnDraft;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.DeletePurchaseReturnDraft;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnPost;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturns;
using InventoryManagementSystem.Domain.Enums;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetReturnableReceiptItems;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/purchase-returns")]
[ApiController]
public class PurchaseReturnsController : ApiControllerBase
{
    [HttpGet("last-number")]
    [EndpointSummary("Get the last purchase return number for frontend numbering")]
    public async Task<IActionResult> GetLastNumber([FromQuery] int year, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetLastPurchaseReturnNumberQuery(year), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Data = result
        });
    }

    [HttpGet("returnable-items")]
    [EndpointSummary("Get received GRN lines with remaining returnable quantity for a supplier")]
    public async Task<IActionResult> GetReturnableItems(
        [FromQuery] GetReturnableReceiptItemsQuery request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(request, cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Returnable receipt items retrieved successfully.",
            Data = result
        });
    }

    [HttpGet]
    [EndpointSummary("Get purchase returns with search, filters, sorting, and pagination")]
    public async Task<IActionResult> GetPurchaseReturns(
        [FromQuery] GetPurchaseReturnsQuery request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(request, cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase returns retrieved successfully.",
            Data = result
        });
    }

    [HttpPost("{id:guid}/post")]
    [EndpointSummary("Post a purchase return draft and deduct stock")]
    public async Task<IActionResult> PostPurchaseReturn(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreatePurchaseReturnPostCommand(id), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase return posted successfully.",
            Data = result
        });
    }

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

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a purchase return draft")]
    public async Task<IActionResult> UpdatePurchaseReturn(Guid id,
        [FromBody] UpdatePurchaseReturnCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("Return ID does not match the route.");
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase return draft updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Soft delete a purchase return draft")]
    public async Task<IActionResult> DeletePurchaseReturn(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeletePurchaseReturnCommand(id), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase return draft deleted successfully."
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

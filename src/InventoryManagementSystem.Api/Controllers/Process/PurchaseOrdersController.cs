using System;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CancelPurchaseOrder;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.DeletePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.SendPurchaseOrderEmail;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.ExportPurchaseOrders;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderById;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailPreview;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderEmailStatus;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrders;
using InventoryManagementSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/purchase-orders")]
[ApiController]
public class PurchaseOrdersController : ApiControllerBase
{
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id, [FromBody] CancelPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest(new DefaultResponseModel
        {
            Success = false, StatusCode = 400, Message = "Purchase order ID mismatch."
        });
        var found = await Mediator.Send(command, cancellationToken);
        if (!found) return NotFound(new DefaultResponseModel
        {
            Success = false, StatusCode = 404, Message = "Purchase order not found."
        });
        return Ok(new DefaultResponseModel
        {
            Success = true, StatusCode = 200, Message = "Purchase order cancelled."
        });
    }

    [HttpGet]
    [EndpointSummary("Get all purchase orders with server-side pagination, search, and sorting")]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] string? q,
        [FromQuery] DateTime? orderDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] bool? excludeCompleted,
        [FromQuery] string? sortField,
        [FromQuery] int order = -1,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchaseOrdersQuery(
            q,
            orderDate,
            supplierId,
            warehouseId,
            status,
            excludeCompleted,
            sortField,
            order,
            pageNumber,
            pageSize));

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase orders retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a purchase order by ID")]
    public async Task<IActionResult> GetPurchaseOrderById(Guid id)
    {
        var order = await Mediator.Send(new GetPurchaseOrderByIdQuery(id));
        if (order == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Purchase order with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase order retrieved successfully.",
            Data = order
        });
    }

    [HttpGet("{id:guid}/email-preview")]
    [EndpointSummary("Preview a purchase order email without sending it")]
    public async Task<IActionResult> GetPurchaseOrderEmailPreview(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPurchaseOrderEmailPreviewQuery(id), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase order email preview retrieved successfully.",
            Data = result
        });
    }

    [HttpPost("{id:guid}/send-email")]
    [EndpointSummary("Send a purchase order email to its supplier")]
    public async Task<IActionResult> SendPurchaseOrderEmail(Guid id, [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey, CancellationToken cancellationToken)
    {
        var emailId = await Mediator.Send(new SendPurchaseOrderEmailCommand(id, idempotencyKey ?? Guid.NewGuid()), cancellationToken);
        return AcceptedAtAction(nameof(GetPurchaseOrderEmailStatus), new { id, emailId }, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status202Accepted,
            Success = true,
            Message = "Purchase order email request accepted. Check its status for the sending result.",
            Data = new { EmailId = emailId }
        });
    }

    [HttpGet("{id:guid}/emails/{emailId:guid}")]
    public async Task<IActionResult> GetPurchaseOrderEmailStatus(Guid id, Guid emailId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPurchaseOrderEmailStatusQuery(id, emailId), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Email status retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create a new purchase order")]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = result.Id }, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Purchase order created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing purchase order")]
    public async Task<IActionResult> UpdatePurchaseOrder(Guid id, [FromBody] UpdatePurchaseOrderCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Purchase order ID mismatch.",
                Data = null
            });
        }

        var result = await Mediator.Send(command);
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Purchase order with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase order updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a purchase order")]
    public async Task<IActionResult> DeletePurchaseOrder(Guid id)
    {
        var result = await Mediator.Send(new DeletePurchaseOrderCommand(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Purchase order with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase order deleted successfully.",
            Data = result
        });
    }

    [HttpGet("export")]
    [EndpointSummary("Export purchase orders to Excel or CSV")]
    public async Task<IActionResult> ExportPurchaseOrders(
        [FromQuery] string? q,
        [FromQuery] DateTime? orderDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] PurchaseOrderStatus? status,
        [FromQuery] string format = "excel",
        [FromQuery] string fontName = "Pyidaungsu")
    {
        var result = await Mediator.Send(new ExportPurchaseOrdersQuery(
            q,
            orderDate,
            supplierId,
            warehouseId,
            status,
            format,
            fontName));

        return File(result.Content, result.ContentType, result.FileName);
    }
}

using System;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.DeletePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.ExportPurchaseOrders;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrderById;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Queries.GetPurchaseOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/purchase-orders")]
[ApiController]
public class PurchaseOrdersController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all purchase orders with server-side pagination, search, and sorting")]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] string? q,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] bool? status,
        [FromQuery] string? sortField,
        [FromQuery] int order = -1,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetPurchaseOrdersQuery(
            q,
            startDate,
            endDate,
            supplierId,
            warehouseId,
            status,
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
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] bool? status,
        [FromQuery] string format = "excel",
        [FromQuery] string fontName = "Pyidaungsu")
    {
        var result = await Mediator.Send(new ExportPurchaseOrdersQuery(
            q,
            startDate,
            endDate,
            supplierId,
            warehouseId,
            status,
            format,
            fontName));

        return File(result.Content, result.ContentType, result.FileName);
    }
}

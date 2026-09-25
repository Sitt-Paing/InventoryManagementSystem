using System;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.GoodReceipts.Commands.CreateGoodReceipt;
using InventoryManagementSystem.Application.Process.GoodReceipts.Commands.DeleteGoodReceipt;
using InventoryManagementSystem.Application.Process.GoodReceipts.Commands.UpdateGoodReceipt;
using InventoryManagementSystem.Application.Process.GoodReceipts.DTOs;
using InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceiptById;
using InventoryManagementSystem.Application.Process.GoodReceipts.Queries.GetGoodReceipts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/good-receipts")]
[ApiController]
public class GoodReceiptsController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all goods receipts with server-side pagination, search, and sorting")]
    public async Task<IActionResult> GetGoodReceipts(
        [FromQuery] string? q,
        [FromQuery] DateTime? receiptDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] bool? status,
        [FromQuery] string? sortField,
        [FromQuery] int order = -1,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetGoodReceiptsQuery(
            q,
            receiptDate,
            supplierId,
            warehouseId,
            purchaseOrderId,
            status,
            sortField,
            order,
            pageNumber,
            pageSize));

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Goods receipts retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a goods receipt by ID")]
    public async Task<IActionResult> GetGoodReceiptById(Guid id)
    {
        var receipt = await Mediator.Send(new GetGoodReceiptByIdQuery(id));
        if (receipt == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Goods receipt with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Goods receipt retrieved successfully.",
            Data = receipt
        });
    }

    [HttpPost]
    [EndpointSummary("Create a new goods receipt")]
    public async Task<IActionResult> CreateGoodReceipt([FromBody] CreateGoodReceiptCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetGoodReceiptById), new { id = result.Id }, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Goods receipt created successfully.",
            Data = result
        });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing goods receipt")]
    public async Task<IActionResult> UpdateGoodReceipt(Guid id, [FromBody] UpdateGoodReceiptCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Goods receipt ID mismatch.",
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
                Message = $"Goods receipt with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Goods receipt updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a goods receipt")]
    public async Task<IActionResult> DeleteGoodReceipt(Guid id)
    {
        var result = await Mediator.Send(new DeleteGoodReceiptCommand(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Goods receipt with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Goods receipt deleted successfully.",
            Data = result
        });
    }

    [HttpGet("export")]
    [EndpointSummary("Export goods receipts to Excel or CSV")]
    public async Task<IActionResult> ExportGoodReceipts(
        [FromQuery] string? q,
        [FromQuery] DateTime? receiptDate,
        [FromQuery] int? supplierId,
        [FromQuery] int? warehouseId,
        [FromQuery] Guid? purchaseOrderId,
        [FromQuery] bool? status,
        [FromQuery] string format = "excel",
        [FromQuery] string fontName = "Pyidaungsu")
    {
        var result = await Mediator.Send(new InventoryManagementSystem.Application.Process.GoodReceipts.Queries.ExportGoodReceipts.ExportGoodReceiptsQuery(
            q,
            receiptDate,
            supplierId,
            warehouseId,
            purchaseOrderId,
            status,
            format,
            fontName));

        return File(result.Content, result.ContentType, result.FileName);
    }
}

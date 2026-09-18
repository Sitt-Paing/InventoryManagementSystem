using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.DeleteStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.UpdateStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactionById;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/stock-transactions")]
[ApiController]
public class StockTransactionsController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all stock transactions with optional filters")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? date,
        [FromQuery] Guid? productId,
        [FromQuery] int? warehouseId)
    {
        var result = await Mediator.Send(new GetStockTransactionsQuery(transactionType, date, productId, warehouseId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Stock transactions retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Get stock transaction by ID")]
    public async Task<IActionResult> GetTransactionById(long id)
    {
        var result = await Mediator.Send(new GetStockTransactionByIdQuery(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Stock transaction with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Stock transaction retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Record a new stock transaction (IN, OUT, ADJUSTMENT)")]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateStockTransactionsCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            command = command with { UserId = currentUserId };
        }

        try
        {
            var result = await Mediator.Send(command);
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = "Stock transaction recorded successfully.",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [HttpPut("{id:long}")]
    [EndpointSummary("Update an existing stock transaction and adjust stock balances")]
    public async Task<IActionResult> UpdateTransaction(long id, [FromBody] UpdateStockTransactionsCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Transaction ID mismatch between route and payload.",
                Data = null
            });
        }

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
            command = command with { UserId = currentUserId };
        }

        try
        {
            var result = await Mediator.Send(command);
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Stock transaction updated successfully.",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete (soft-delete) a stock transaction and revert stock balances")]
    public async Task<IActionResult> DeleteTransaction(long id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";

        try
        {
            var result = await Mediator.Send(new DeleteStockTransactionsCommand(id, currentUserId));
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Stock transaction deleted successfully.",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }
}

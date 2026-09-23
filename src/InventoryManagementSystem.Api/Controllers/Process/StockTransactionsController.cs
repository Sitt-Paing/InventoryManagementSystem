using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.CreateStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.DeleteStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Command.UpdateStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.DTOs;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactionById;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetStockTransactions;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetWarehouseStockBalance;
using InventoryManagementSystem.Application.Process.StockTransactions.Queries.GetProductWarehouseStocks;
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
    [EndpointSummary("Get all stock transactions with server-side pagination, search, and sorting")]
    public async Task<IActionResult> GetStockTransactions(
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? q,
        [FromQuery] string? sortField,
        [FromQuery] int order = -1,
        [FromQuery] Guid? productId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(new GetStockTransactionsQuery(
            transactionType,
            date,
            startDate,
            endDate,
            q,
            sortField,
            order,
            productId,
            warehouseId,
            pageNumber,
            pageSize));

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Stock transactions retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("warehouse-balance")]
    [EndpointSummary("Get current available stock balance of a product in a warehouse")]
    public async Task<IActionResult> GetWarehouseStockBalance(
        [FromQuery] Guid productId,
        [FromQuery] int warehouseId)
    {
        var result = await Mediator.Send(new GetWarehouseStockBalanceQuery(productId, warehouseId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse stock balance retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("product-warehouse-stocks/{productId:guid}")]
    [EndpointSummary("Get stock balance across all warehouses for a specific product")]
    public async Task<IActionResult> GetProductWarehouseStocks(Guid productId)
    {
        var result = await Mediator.Send(new GetProductWarehouseStocksQuery(productId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Product warehouse stocks retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Get stock transaction by ID")]
    public async Task<IActionResult> GetStockTransactionById(long id)
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
    public async Task<IActionResult> CreateStockTransaction([FromBody] CreateStockTransactionsCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                command = command with { UserId = currentUserId };
            }
        }

        var result = await Mediator.Send(command);
        var response = new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Stock transaction recorded successfully.",
            Data = result
        };

        return CreatedAtAction(nameof(GetStockTransactionById), new { id = result.Id }, response);
    }

    [HttpPut("{id:long}")]
    [EndpointSummary("Update an existing stock transaction and adjust stock balances")]
    public async Task<IActionResult> UpdateStockTransaction(long id, [FromBody] UpdateStockTransactionsCommand command)
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
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                command = command with { UserId = currentUserId };
            }
        }

        var result = await Mediator.Send(command);
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
            Message = "Stock transaction updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete (soft-delete) a stock transaction and revert stock balances")]
    public async Task<IActionResult> DeleteStockTransaction(long id)
    {
        if (id <= 0)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Invalid stock transaction ID.",
                Data = null
            });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        var result = await Mediator.Send(new DeleteStockTransactionsCommand(id, currentUserId));
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
            Message = "Stock transaction deleted successfully.",
            Data = result
        });
    }
}

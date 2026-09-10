using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Warehouses.Commands.CreateWarehouse;
using InventoryManagementSystem.Application.Warehouses.Commands.DeleteWarehouse;
using InventoryManagementSystem.Application.Warehouses.Commands.UpdateWarehouse;
using InventoryManagementSystem.Application.Warehouses.DTOs;
using InventoryManagementSystem.Application.Warehouses.Queries.GetWarehouseById;
using InventoryManagementSystem.Application.Warehouses.Queries.GetWarehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers;

[Authorize]
[Route("api/warehouses")]
[ApiController]
public class WarehousesController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all warehouses")]
    public async Task<IActionResult> GetWarehouses()
    {
        var warehouses = await Mediator.Send(new GetWarehousesQuery());
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouses retrieved successfully.",
            Data = warehouses
        });
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get warehouse by ID")]
    public async Task<IActionResult> GetWarehouseById(int id)
    {
        var warehouse = await Mediator.Send(new GetWarehouseByIdQuery(id));
        if (warehouse == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Warehouse with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse retrieved successfully.",
            Data = warehouse
        });
    }

    [HttpPost]
    [EndpointSummary("Create new warehouse")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command)
    {
        var warehouse = await Mediator.Send(command);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Warehouse created successfully.",
            Data = warehouse
        });
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update warehouse")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Warehouse ID mismatch.",
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
                Message = $"Warehouse with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete warehouse")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Invalid warehouse ID.",
                Data = null
            });
        }

        var result = await Mediator.Send(new DeleteWarehouseCommand(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Warehouse with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse deleted successfully.",
            Data = result
        });
    }
}

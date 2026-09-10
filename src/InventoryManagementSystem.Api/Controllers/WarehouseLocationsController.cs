using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.WarehouseLocations.Commands.CreateWarehouseLocation;
using InventoryManagementSystem.Application.WarehouseLocations.Commands.DeleteWarehouseLocation;
using InventoryManagementSystem.Application.WarehouseLocations.Commands.UpdateWarehouseLocation;
using InventoryManagementSystem.Application.WarehouseLocations.DTOs;
using InventoryManagementSystem.Application.WarehouseLocations.Queries.GetWarehouseLocationById;
using InventoryManagementSystem.Application.WarehouseLocations.Queries.GetWarehouseLocations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers;

[Authorize]
[Route("api/warehouse-locations")]
[ApiController]
public class WarehouseLocationsController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all warehouse locations, optionally filtered by warehouse ID")]
    public async Task<IActionResult> GetWarehouseLocations([FromQuery] int? warehouseId)
    {
        var locations = await Mediator.Send(new GetWarehouseLocationsQuery(warehouseId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse locations retrieved successfully.",
            Data = locations
        });
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get warehouse location by ID")]
    public async Task<IActionResult> GetWarehouseLocationById(int id)
    {
        var location = await Mediator.Send(new GetWarehouseLocationByIdQuery(id));
        if (location == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Warehouse location with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse location retrieved successfully.",
            Data = location
        });
    }

    [HttpPost]
    [EndpointSummary("Create new warehouse location")]
    public async Task<IActionResult> CreateWarehouseLocation([FromBody] CreateWarehouseLocationCommand command)
    {
        var location = await Mediator.Send(command);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Warehouse location created successfully.",
            Data = location
        });
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update warehouse location")]
    public async Task<IActionResult> UpdateWarehouseLocation(int id, [FromBody] UpdateWarehouseLocationCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Warehouse location ID mismatch.",
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
                Message = $"Warehouse location with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse location updated successfully.",
            Data = result
        });
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete warehouse location")]
    public async Task<IActionResult> DeleteWarehouseLocation(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Invalid warehouse location ID.",
                Data = null
            });
        }

        var result = await Mediator.Send(new DeleteWarehouseLocationCommand(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Warehouse location with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Warehouse location deleted successfully.",
            Data = result
        });
    }
}

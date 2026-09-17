using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.CreateUnitOfMeasure;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.DeleteUnitOfMeasure;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.Commands.UpdateUnitOfMeasure;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasureById;
using InventoryManagementSystem.Application.Master.UnitOfMeasures.Queries.GetUnitOfMeasures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers.Master;

[Authorize]
[Route("api/master/unit-of-measures")]
[ApiController]
public class UnitOfMeasuresController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all Unit of Measures, optionally filtered by Category")]
    public async Task<IActionResult> GetUnitOfMeasures([FromQuery] long? categoryId = null)
    {
        var result = await Mediator.Send(new GetUnitOfMeasuresQuery(categoryId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Unit of Measures retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Get Unit of Measure by Id")]
    public async Task<IActionResult> GetUnitOfMeasureById(long id)
    {
        var result = await Mediator.Send(new GetUnitOfMeasureByIdQuery(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Unit of Measure with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Unit of Measure retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create new Unit of Measure")]
    public async Task<IActionResult> CreateUnitOfMeasure([FromBody] CreateUnitOfMeasureCommand command)
    {
        var dto = await Mediator.Send(command);
        var response = new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Unit of Measure created successfully.",
            Data = dto
        };

        return CreatedAtAction(nameof(GetUnitOfMeasureById), new { id = dto.Id }, response);
    }

    [HttpPut("{id:long}")]
    [EndpointSummary("Update Unit of Measure")]
    public async Task<IActionResult> UpdateUnitOfMeasure(long id, [FromBody] UpdateUnitOfMeasureCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Mismatched Unit of Measure Id in route and body.",
                Data = null
            });
        }

        var updated = await Mediator.Send(command);
        if (updated == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Unit of Measure with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Unit of Measure updated successfully.",
            Data = updated
        });
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete Unit of Measure")]
    public async Task<IActionResult> DeleteUnitOfMeasure(long id)
    {
        var deleted = await Mediator.Send(new DeleteUnitOfMeasureCommand(id));
        if (deleted == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Unit of Measure with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Unit of Measure deleted successfully.",
            Data = deleted
        });
    }
}

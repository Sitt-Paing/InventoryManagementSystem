using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.UomCategories.Commands.CreateUomCategory;
using InventoryManagementSystem.Application.Master.UomCategories.Commands.DeleteUomCategory;
using InventoryManagementSystem.Application.Master.UomCategories.Commands.UpdateUomCategory;
using InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategories;
using InventoryManagementSystem.Application.Master.UomCategories.Queries.GetUomCategoryById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers.Master;

[Authorize]
[Route("api/master/uom-categories")]
[ApiController]
public class UomCategoriesController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all UOM categories")]
    public async Task<IActionResult> GetUomCategories()
    {
        var result = await Mediator.Send(new GetUomCategoriesQuery());
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "UOM Categories retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id:long}")]
    [EndpointSummary("Get UOM category by Id")]
    public async Task<IActionResult> GetUomCategoryById(long id)
    {
        var result = await Mediator.Send(new GetUomCategoryByIdQuery(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"UOM Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "UOM Category retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create new UOM category")]
    public async Task<IActionResult> CreateUomCategory([FromBody] CreateUomCategoryCommand command)
    {
        var dto = await Mediator.Send(command);
        var response = new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "UOM Category created successfully.",
            Data = dto
        };

        return CreatedAtAction(nameof(GetUomCategoryById), new { id = dto.Id }, response);
    }

    [HttpPut("{id:long}")]
    [EndpointSummary("Update UOM category")]
    public async Task<IActionResult> UpdateUomCategory(long id, [FromBody] UpdateUomCategoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Mismatched UOM Category Id in route and body.",
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
                Message = $"UOM Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "UOM Category updated successfully.",
            Data = updated
        });
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete UOM category")]
    public async Task<IActionResult> DeleteUomCategory(long id)
    {
        var deleted = await Mediator.Send(new DeleteUomCategoryCommand(id));
        if (deleted == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"UOM Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "UOM Category deleted successfully.",
            Data = deleted
        });
    }
}

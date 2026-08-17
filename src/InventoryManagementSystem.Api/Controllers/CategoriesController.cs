using InventoryManagementSystem.Application.Categories.Commands.CreateCategory;
using InventoryManagementSystem.Application.Categories.Commands.DeleteCategory;
using InventoryManagementSystem.Application.Categories.Commands.UpdateCategory;
using InventoryManagementSystem.Application.Categories.DTOs;
using InventoryManagementSystem.Application.Categories.Queries.GetCategories;
using InventoryManagementSystem.Application.Categories.Queries.GetCategoryById;
using InventoryManagementSystem.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers;

[Authorize]
[Route("api/categories")]
[ApiController]
public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get all categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await Mediator.Send(new GetCategoriesQuery());
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Categories retrieved successfully.",
            Data = result
        });
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get category by Id")]
    public async Task<IActionResult> GetCategoryById(long id)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id));
        if (result == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Category retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create new category")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var categoryDto = await Mediator.Send(command);
        var response = new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Category created successfully.",
            Data = categoryDto
        };

        return CreatedAtAction(nameof(GetCategoryById), new { id = categoryDto.Id }, response);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update category")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Mismatched Category Id in route and body.",
                Data = null
            });
        }

        var updatedCategory = await Mediator.Send(command);
        if (updatedCategory == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Category updated successfully.",
            Data = updatedCategory
        });
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete Category")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        var deletedCategory = await Mediator.Send(new DeleteCategoryCommand(id));
        if (deletedCategory == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Category with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Category deleted successfully.",
            Data = deletedCategory
        });
    }
}

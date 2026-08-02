using InventoryManagementSystem.Application.Categories.Commands;
using InventoryManagementSystem.Application.Categories.DTOs;
using InventoryManagementSystem.Application.Categories.Queries;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers;

public class CategoriesController : ApiControllerBase
{
    // GET: api/categories
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        return await Mediator.Send(new GetCategoriesQuery());
    }

    // GET: api/categories/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(long id)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id));
        if (result == null) return NotFound();

        return Ok(result);
    }

    // POST: api/categories
    [HttpPost]
    public async Task<ActionResult<long>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var categoryId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetCategoryById), new { id = categoryId }, categoryId);
    }

    // PUT: api/categories/5
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id) return BadRequest("Mismatched Category Id in route and body.");

        var success = await Mediator.Send(command);
        if (!success) return NotFound();

        return NoContent();
    }

    // DELETE: api/categories/5
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        var success = await Mediator.Send(new DeleteCategoryCommand(id));
        if (!success) return NotFound();

        return NoContent();
    }
}

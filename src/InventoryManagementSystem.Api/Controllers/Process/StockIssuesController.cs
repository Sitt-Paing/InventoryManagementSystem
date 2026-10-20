using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssueDraft;
using InventoryManagementSystem.Application.Process.StockIssues.Commands.CreateStockIssuePost;
using InventoryManagementSystem.Application.Process.StockIssues.Commands.DeleteStockIssueDraft;
using InventoryManagementSystem.Application.Process.StockIssues.Commands.UpdateStockIssueDraft;
using InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssueById;
using InventoryManagementSystem.Application.Process.StockIssues.Queries.GetStockIssues;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/stock-issues")]
[ApiController]
public class StockIssuesController : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("Get stock issues with search, filters and pagination")]
    public async Task<IActionResult> Get([FromQuery] GetStockIssuesQuery request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(request, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a stock issue by ID")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetStockIssueByIdQuery(id), cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }

    [HttpPost]
    [EndpointSummary("Create a stock issue draft")]
    public async Task<IActionResult> Create([FromBody] CreateStockIssueCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new DefaultResponseModel
        {
            StatusCode = 201, Success = true, Message = "Stock issue draft saved successfully.", Data = result
        });
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a stock issue draft")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockIssueCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("Issue ID does not match the route.");
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Message = "Stock issue draft updated successfully.", Data = result });
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a stock issue draft")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await Mediator.Send(new DeleteStockIssueCommand(id), cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Message = "Stock issue draft deleted successfully." });
    }

    [HttpPost("{id:guid}/post")]
    [EndpointSummary("Post a stock issue and deduct warehouse stock")]
    public async Task<IActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateStockIssuePostCommand(id), cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Message = "Stock issue posted successfully.", Data = result });
    }
}

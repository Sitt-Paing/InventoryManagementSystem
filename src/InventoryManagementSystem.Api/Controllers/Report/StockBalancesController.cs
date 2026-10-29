using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Report.StockBalances.Queries.GetStockBalances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Report
{
    [Authorize(Policy = "InternalUser")]
    [Route("api/report/stock-balances")]
    [ApiController]
    public class StockBalancesController : ApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get the current stock balance report")]
        [EndpointDescription("StartDate and EndDate filter StockCreatedOn, including both dates. Quantities are current balances, not historical balances.")]
        public async Task<IActionResult> GetStockBalances([FromQuery] GetStockBalancesQuery request, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(request, cancellationToken);
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Stock balances retrieved successfully.",
                Data = result
            });
        }
    }
}

using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.Dashboard.Queries.GetSupplierDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.SupplierPortal;

[ApiController]
[Authorize(Roles = "Supplier")]
[Route("api/supplier/dashboard")]
public class SupplierDashboardController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetSupplierDashboardQuery query, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }
}

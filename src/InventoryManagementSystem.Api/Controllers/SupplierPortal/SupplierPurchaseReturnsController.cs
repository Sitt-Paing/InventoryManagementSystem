using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.PurchaseReturns.Queries.GetSupplierPurchaseReturns;
using InventoryManagementSystem.Application.SupplierPortal.PurchaseReturns.Queries.GetSupplierPurchaseReturnById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.SupplierPortal;

[ApiController]
[Authorize(Roles = "Supplier")]
[Route("api/supplier/purchase-returns")]
public class SupplierPurchaseReturnsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetSupplierPurchaseReturnsQuery query, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSupplierPurchaseReturnByIdQuery(id), cancellationToken);
        if (result == null)
            return NotFound(new DefaultResponseModel { StatusCode = 404, Success = false, Message = "Document not found." });
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }
}

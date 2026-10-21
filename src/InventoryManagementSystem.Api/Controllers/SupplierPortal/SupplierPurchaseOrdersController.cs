using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.PurchaseOrders.Queries.GetSupplierPurchaseOrders;
using InventoryManagementSystem.Application.SupplierPortal.PurchaseOrders.Queries.GetSupplierPurchaseOrderById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.SupplierPortal;

[ApiController]
[Authorize(Roles = "Supplier")]
[Route("api/supplier/purchase-orders")]
public class SupplierPurchaseOrdersController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetSupplierPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSupplierPurchaseOrderByIdQuery(id), cancellationToken);
        if (result == null)
            return NotFound(new DefaultResponseModel { StatusCode = 404, Success = false, Message = "Document not found." });
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }
}

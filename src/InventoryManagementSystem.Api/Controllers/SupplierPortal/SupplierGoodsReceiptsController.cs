using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.SupplierPortal.GoodReceipts.Queries.GetSupplierGoodsReceipts;
using InventoryManagementSystem.Application.SupplierPortal.GoodReceipts.Queries.GetSupplierGoodReceiptById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.SupplierPortal;

[ApiController]
[Authorize(Roles = "Supplier")]
[Route("api/supplier/goods-receipts")]
public class SupplierGoodsReceiptsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetSupplierGoodsReceiptsQuery query, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSupplierGoodReceiptByIdQuery(id), cancellationToken);
        if (result == null)
            return NotFound(new DefaultResponseModel { StatusCode = 404, Success = false, Message = "Document not found." });
        return Ok(new DefaultResponseModel { StatusCode = 200, Success = true, Data = result });
    }
}

using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.ProductUomConversions.Commands.CreateProductUomConversion;
using InventoryManagementSystem.Application.ProductUomConversions.Commands.DeleteProductUomConversion;
using InventoryManagementSystem.Application.ProductUomConversions.Commands.UpdateProductUomConversion;
using InventoryManagementSystem.Application.ProductUomConversions.Queries.GetProductUomConversionsByProductId;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers;

[Authorize]
[Route("api/product-uom-conversions")]
[ApiController]
public class ProductUomConversionsController : ApiControllerBase
{
    [HttpGet("product/{productId:guid}")]
    [EndpointSummary("Get all UOM conversions and packaging barcodes for a product")]
    public async Task<IActionResult> GetByProductId(Guid productId)
    {
        var result = await Mediator.Send(new GetProductUomConversionsByProductIdQuery(productId));
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Product UOM conversions retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create product packaging UOM conversion with barcode")]
    public async Task<IActionResult> Create([FromBody] CreateProductUomConversionCommand command)
    {
        var dto = await Mediator.Send(command);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Product UOM conversion created successfully.",
            Data = dto
        });
    }

    [HttpPut("{id:long}")]
    [EndpointSummary("Update product packaging UOM conversion and barcode")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductUomConversionCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Mismatched Id in route and body.",
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
                Message = $"Product UOM conversion with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Product UOM conversion updated successfully.",
            Data = updated
        });
    }

    [HttpDelete("{id:long}")]
    [EndpointSummary("Delete product packaging UOM conversion")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await Mediator.Send(new DeleteProductUomConversionCommand(id));
        if (deleted == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Product UOM conversion with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Product UOM conversion deleted successfully.",
            Data = deleted
        });
    }
}

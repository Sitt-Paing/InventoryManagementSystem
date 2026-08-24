using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Products.Commands.CreateProduct;
using InventoryManagementSystem.Application.Products.Commands.DeleteProduct;
using InventoryManagementSystem.Application.Products.Commands.UpdateProduct;
using InventoryManagementSystem.Application.Products.DTOs;
using InventoryManagementSystem.Application.Products.Queries.GetProducts;
using InventoryManagementSystem.Application.Products.Queries.GetProductsById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers
{
    [Authorize]
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all products or filter by category")]
        public async Task<IActionResult> GetProducts([FromQuery] long? categoryId = null)
        {
            List<ProductDto> result = await Mediator.Send(new GetProductsQuery(categoryId));
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Products retrieved successfully.",
                Data = result
            });
        }

        [HttpGet("export")]
        [EndpointSummary("Export products to Excel or CSV")]
        public async Task<IActionResult> ExportProducts(
            [FromQuery] long? categoryId = null,
            [FromQuery] string format = "excel",
            [FromQuery] string fontName = "Pyidaungsu")
        {
            var result = await Mediator.Send(new InventoryManagementSystem.Application.Products.Queries.ExportProducts.ExportProductsQuery(categoryId, format, fontName));
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpPost("excel")]
        [EndpointSummary("Export Excel")]
        [EndpointDescription("Product List Export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> PostExcelAsync(
            [FromQuery] long? categoryId,
            [FromQuery] string? q,
            [FromQuery] string? sortField,
            [FromQuery] int? order,
            [FromBody] KeyValuePair<string, string>[] columns)
        {
            var products = await Mediator.Send(new GetProductsQuery(categoryId));

            if (!string.IsNullOrWhiteSpace(q))
            {
                products = products.Where(p =>
                    p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Sku != null && p.Sku.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Barcode != null && p.Barcode.Contains(q, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (products.Count == 0)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Failed to export excel: No data found.",
                    Data = null
                });
            }

            Stream? stream = ExportService.ExportToExcelStreamSpecificColumns(products, columns, "Product List", "Pyidaungsu");

            if (stream == null)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Failed to export excel.",
                    Data = null
                });
            }

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Product List.xlsx");
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get product by Id")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            ProductDto result = await Mediator.Send(new GetProductByIdQuery(id));
            if (result == null)
            {
                return NotFound(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Success = false,
                    Message = $"Product with ID {id} not found.",
                    Data = null
                });
            }
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Product retrieved successfully.",
                Data = result
            });
        }

        [HttpPost]
        [EndpointSummary("Create new product")]
        public async Task<IActionResult> CreateProduct(CreateProductCommand command)
        {
            ProductDto productDto = await Mediator.Send(command);
            DefaultResponseModel response = new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = "Product created successfully.",
                Data = productDto
            };
            return CreatedAtAction(nameof(GetProductById), new { id = productDto.Id }, response);
        }

        [HttpPut("{id}")]
        [EndpointSummary("Update product by Id")]
        public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductCommand command)
        {
            if (id != command.Id)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Product ID mismatch.",
                    Data = null
                });
            }

            ProductDto? updatedProduct = await Mediator.Send(command);
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Product updated successfully.",
                Data = updatedProduct
            });
        }

        [HttpDelete("{id}")]
        [EndpointSummary("Delete product by Id")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var deletedProduct = await Mediator.Send(new DeleteProductCommand(id));
            if (deletedProduct == null)
            {
                return NotFound(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Success = false,
                    Message = $"Product with ID {id} not found.",
                    Data = null
                });
            }
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Product deleted successfully.",
                Data = null
            });
        }
    }
}

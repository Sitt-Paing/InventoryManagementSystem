using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Products.Commands.CreateProduct;
using InventoryManagementSystem.Application.Products.Commands.DeleteProduct;
using InventoryManagementSystem.Application.Products.Commands.UpdateProduct;
using InventoryManagementSystem.Application.Products.DTOs;
using InventoryManagementSystem.Application.Products.Queries.GetProducts;
using InventoryManagementSystem.Application.Products.Queries.GetProductsById;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all products")]
        public async Task<IActionResult> GetProducts()
        {
            List<ProductDto> result = await Mediator.Send(new GetProductsQuery());
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Products retrieved successfully.",
                Data = result
            });
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

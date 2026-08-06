using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Products.Commands.CreateProduct;
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
        public async Task<IActionResult> GetProductById(string id)
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
    }
}

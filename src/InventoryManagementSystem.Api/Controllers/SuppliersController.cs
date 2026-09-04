using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Suppliers.Commands.CreateSupplier;
using InventoryManagementSystem.Application.Suppliers.DTOs;
using InventoryManagementSystem.Application.Suppliers.Queries.GetSuppliers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers
{
    [Route("api/suppliers")]
    [ApiController]
    public class SuppliersController : ApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all suppliers")]
        public async Task<IActionResult> GetAllSuppliers()
        {
            var suppliers = await Mediator.Send(new GetSuppliersQuery());
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Suppliers retrieved successfully.",
                Data = suppliers
            });
        } 

        [HttpPost]
        [EndpointSummary("Create a new supplier")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command)
        {
            SupplierDto result = await Mediator.Send(command);
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = "Supplier created successfully.",
                Data = result
            });
        }
    }
}

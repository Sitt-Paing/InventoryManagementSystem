using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.Suppliers.Commands.CreateSupplier;
using InventoryManagementSystem.Application.Master.Suppliers.Commands.DeleteSupplier;
using InventoryManagementSystem.Application.Master.Suppliers.Commands.UpdateSupplier;
using InventoryManagementSystem.Application.Master.Suppliers.DTOs;
using InventoryManagementSystem.Application.Master.Suppliers.Queries.GetSupplierById;
using InventoryManagementSystem.Application.Master.Suppliers.Queries.GetSuppliers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Master
{
    [Authorize]
    [Route("api/master/suppliers")]
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

        [HttpGet("{id}")]
        [EndpointSummary("Get a supplier by ID")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            SupplierDto? supplier = await Mediator.Send(new GetSupplierByIdQuery(id));
            if (supplier == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Supplier retrieved successfully.",
                Data = supplier
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

        [HttpPut("{id}")]
        [EndpointSummary("Update an existing supplier")]
        public async Task<IActionResult> UpdateSupplier(int id, UpdateSupplierCommand command)
        {
            if (id != command.id)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Supplier ID mismatch.",
                    Data = null
                });
            }
            SupplierDto? result = await Mediator.Send(command);
            if (result == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Supplier updated successfully.",
                Data = result
            });
        }

        [HttpDelete("{id}")]
        [EndpointSummary("Delete a supplier")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if(id <= 0)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Invalid supplier ID.",
                    Data = null
                });
            }

            SupplierDto result = await Mediator.Send(new DeleteSupplierCommand(id));
            if (result == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Supplier deleted successfully.",
                Data = result
            });
        }
    }
}

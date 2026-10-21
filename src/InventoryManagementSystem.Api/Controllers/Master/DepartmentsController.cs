using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.Departments.Commands.CreateDepartment;
using InventoryManagementSystem.Application.Master.Departments.Commands.DeleteDepartment;
using InventoryManagementSystem.Application.Master.Departments.Commands.UpdateDepartment;
using InventoryManagementSystem.Application.Master.Departments.DTOs;
using InventoryManagementSystem.Application.Master.Departments.Queries.GetDepartmentById;
using InventoryManagementSystem.Application.Master.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Master
{
    [Authorize(Policy = "InternalUser")]
    [Route("api/master/departments")]
    [ApiController]
    public class DepartmentsController : ApiControllerBase
    {
        [HttpGet]
        [EndpointSummary("Get all departments")]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await Mediator.Send(new GetDepartmentsQuery());
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Departments retrieved successfully.",
                Data = departments
            });
        }

        [HttpGet("{id}")]
        [EndpointSummary("Get a department by ID")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            DepartmentDto? department = await Mediator.Send(new GetDepartmentByIdQuery(id));
            if (department == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Department retrieved successfully.",
                Data = department
            });
        }

        [HttpPost]
        [EndpointSummary("Create a new department")]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentCommand command)
        {
            DepartmentDto result = await Mediator.Send(command);
            return StatusCode(StatusCodes.Status201Created, new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Success = true,
                Message = "Department created successfully.",
                Data = result
            });
        }

        [HttpPut("{id}")]
        [EndpointSummary("Update an existing department")]
        public async Task<IActionResult> UpdateDepartment(int id, UpdateDepartmentCommand command)
        {
            if (id != command.id)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Department ID mismatch.",
                    Data = null
                });
            }
            DepartmentDto? result = await Mediator.Send(command);
            if (result == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Department updated successfully.",
                Data = result
            });
        }

        [HttpDelete("{id}")]
        [EndpointSummary("Delete a department")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            if(id <= 0)
            {
                return BadRequest(new DefaultResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Success = false,
                    Message = "Invalid department ID.",
                    Data = null
                });
            }

            DepartmentDto? result = await Mediator.Send(new DeleteDepartmentCommand(id));
            if (result == null) return NotFound();
            return Ok(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Success = true,
                Message = "Department deleted successfully.",
                Data = result
            });
        }
    }
}

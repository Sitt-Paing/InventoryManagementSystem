using InventoryManagementSystem.Application.Auth.Commands.RegisterCompanyWithAdmin;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Master.Companies.Commands.DeleteCompany;
using InventoryManagementSystem.Application.Master.Companies.Commands.UpdateCompany;
using InventoryManagementSystem.Application.Master.Companies.Queries.GetCompanies;
using InventoryManagementSystem.Application.Master.Companies.Queries.GetCompanyById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace InventoryManagementSystem.Api.Controllers.UserManage;

[Authorize]
[Route("api/usermanage/companies")]
[ApiController]
public class CompaniesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public CompaniesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [EndpointSummary("Get all companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await Mediator.Send(new GetCompaniesQuery());
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Companies retrieved successfully.",
            Data = companies
        });
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Get company by Id")]
    public async Task<IActionResult> GetCompanyById(int id)
    {
        var company = await Mediator.Send(new GetCompanyByIdQuery(id));
        if (company == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Company with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Company retrieved successfully.",
            Data = company
        });
    }

    [HttpPost("register-with-admin")]
    [EndpointSummary("SuperAdmin: Register client company with initial admin user")]
    public async Task<IActionResult> RegisterWithAdmin([FromBody] RegisterCompanyWithAdminCommand command)
    {
        var result = await Mediator.Send(command);
        if (!result.Succeeded)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = result.Message ?? "Failed to register company.",
                Data = result.Errors
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = result.Message,
            Data = result
        });
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update company")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Success = false,
                Message = "Mismatched Company Id in route and body.",
                Data = null
            });
        }

        var updatedCompany = await Mediator.Send(command);
        if (updatedCompany == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Company with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Company updated successfully.",
            Data = updatedCompany
        });
    }

    [HttpPut("{id:int}/toggle-status")]
    [EndpointSummary("Toggle active/inactive status of a company")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id && !c.DeletedOn.HasValue);
        if (company == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Company with ID {id} not found."
            });
        }

        company.IsActive = !company.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = $"Company '{company.CompanyName}' status updated to {(company.IsActive ? "Active" : "Inactive")}.",
            Data = new { company.Id, company.IsActive }
        });
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete company")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var deletedCompany = await Mediator.Send(new DeleteCompanyCommand(id));
        if (deletedCompany == null)
        {
            return NotFound(new DefaultResponseModel
            {
                StatusCode = StatusCodes.Status404NotFound,
                Success = false,
                Message = $"Company with ID {id} not found.",
                Data = null
            });
        }

        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Company deleted successfully.",
            Data = deletedCompany
        });
    }
}

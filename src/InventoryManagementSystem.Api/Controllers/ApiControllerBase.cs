
using InventoryManagementSystem.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    private IExportService? _exportService;
    private IBarcodeGenerationService? _barcodeGenerationService;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    protected IExportService ExportService => _exportService ??= HttpContext.RequestServices.GetRequiredService<IExportService>();
    protected IBarcodeGenerationService BarcodeGenerationService => _barcodeGenerationService ??= HttpContext.RequestServices.GetRequiredService<IBarcodeGenerationService>();
}

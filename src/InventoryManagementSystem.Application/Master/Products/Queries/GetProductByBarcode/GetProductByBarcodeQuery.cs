using InventoryManagementSystem.Application.Master.Products.DTOs;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Products.Queries.GetProductByBarcode;

public record GetProductByBarcodeQuery(string Code) : IRequest<BarcodeLookupResultDto?>;

using InventoryManagementSystem.Application.Common.Models;
using MediatR;

namespace InventoryManagementSystem.Application.Master.Products.Queries.ExportProducts;

public record ExportProductsQuery(
    long? CategoryId = null,
    string Format = "excel",
    string FontName = "Pyidaungsu"
) : IRequest<ExportFileDto>;

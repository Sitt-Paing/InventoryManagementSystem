using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Api.Controllers.Process;

[ApiController]
[Authorize(Policy = "InternalUser")]
[Route("api/process/low-stock-notifications")]
public sealed class LowStockNotificationsController(InventoryManagementDbContext context)
    : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var notifications = await (
            from notification in context.LowStockNotifications.AsNoTracking()
            join product in context.Products.AsNoTracking()
                on notification.ProductId equals product.Id
            where notification.DeletedOn == null && product.DeletedOn == null
            orderby notification.CreatedOn descending, notification.Id descending
            select new
            {
                notification.Id,
                ProductName = product.Name,
                notification.CurrentQuantity,
                notification.ReorderLevel,
                notification.CreatedOn
            })
            .Take(20)
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }
}

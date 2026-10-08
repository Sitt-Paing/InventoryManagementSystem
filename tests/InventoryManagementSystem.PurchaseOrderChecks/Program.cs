using InventoryManagementSystem.Application;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CreatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.UpdatePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.DeletePurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.Commands.CancelPurchaseOrder;
using InventoryManagementSystem.Application.Process.PurchaseOrders.DTOs;
using InventoryManagementSystem.Application.Process.GoodReceipts.Commands.CreateGoodReceipt;
using InventoryManagementSystem.Application.Process.GoodReceipts.Commands.DeleteGoodReceipt;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;

// A uniquely named disposable SQL Server database; never use the application's database.
var database = $"IMS_PO_Checks_{Guid.NewGuid():N}";
var connection = $"Server=.;Database={database};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10";
var identity = new TestUser();
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<ICurrentUserService>(identity);
services.AddDbContext<InventoryManagementDbContext>(o => o.UseSqlServer(connection));
services.AddScoped<IApplicationDbContext>(s => s.GetRequiredService<InventoryManagementDbContext>());
services.AddApplicationServices();
await using var provider = services.BuildServiceProvider();
InventoryManagementDbContext Db(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<InventoryManagementDbContext>();
async Task<T> Send<T>(IRequest<T> request)
{
    using var scope = provider.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
}
var checks = 0;
int supplierId = 0, warehouseId = 0, foreignSupplierId = 0, foreignWarehouseId = 0;
long baseUomId = 0, boxUomId = 0;
Guid productId = Guid.Empty, foreignProductId = Guid.Empty;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    checks++;
    Console.WriteLine($"PASS: {label}");
}
async Task Reject<T>(IRequest<T> request, string label)
{
    try { await Send(request); }
    catch (InvalidOperationException) { Check(true, label); return; }
    catch (FluentValidation.ValidationException) { Check(true, label); return; }
    throw new Exception($"Expected rejection: {label}");
}

try
{
    using (var scope = provider.CreateScope())
    {
        var db = Db(scope);
        await db.Database.EnsureCreatedAsync();
        // Reconstruct the pre-change schema, then exercise the actual new migrations.
        await db.Database.ExecuteSqlRawAsync(@"
            DROP INDEX IX_PurchaseOrders_CompanyId_PurchaseOrderNo ON PurchaseOrders;
            ALTER TABLE PurchaseOrders DROP COLUMN CancellationReason, CreateRequestHash;
            DROP SEQUENCE dbo.PurchaseOrderNumberSequence;
            CREATE TABLE __EFMigrationsHistory (
                MigrationId nvarchar(150) NOT NULL PRIMARY KEY,
                ProductVersion nvarchar(32) NOT NULL);");
        foreach (var migration in db.Database.GetMigrations().Where(m =>
            !m.EndsWith("_HardenPurchaseOrders") && !m.EndsWith("_AddPurchaseOrderNumberSequence")))
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO __EFMigrationsHistory VALUES ({migration}, {"10.0.10"})");
        await db.Database.MigrateAsync();
        Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "new PO migrations apply successfully to existing schema");
        var company = new Company { CompanyName = "PO Test A", IsActive = true };
        var otherCompany = new Company { CompanyName = "PO Test B", IsActive = true };
        db.Companies.AddRange(company, otherCompany);
        await db.SaveChangesAsync();
        identity.CompanyId = company.Id;
        var supplier = new Supplier { SupplierCode = "A", CompanyName = "Supplier A", PaymentTerms = "Net 30", Status = true };
        var otherSupplier = new Supplier { SupplierCode = "B", CompanyName = "Supplier B", PaymentTerms = "Net 30", Status = true, CompanyId = otherCompany.Id };
        var warehouse = new Warehouse { WarehouseCode = "A", Name = "Warehouse A", Status = true };
        var otherWarehouse = new Warehouse { WarehouseCode = "B", Name = "Warehouse B", Status = true, CompanyId = otherCompany.Id };
        var category = new Category { Name = "Goods", IsActive = true };
        var uomCategory = new UomCategory { Name = "Units", IsActive = true };
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.Warehouses.AddRange(warehouse, otherWarehouse);
        db.Categories.Add(category);
        db.UomCategories.Add(uomCategory);
        await db.SaveChangesAsync();
        var unit = new UnitOfMeasure { Code = "PCS", Name = "Piece", CategoryId = uomCategory.Id, IsActive = true };
        var box = new UnitOfMeasure { Code = "BOX", Name = "Box", CategoryId = uomCategory.Id, IsActive = true };
        db.UnitOfMeasures.AddRange(unit, box);
        await db.SaveChangesAsync();
        var product = new Product { Id = Guid.NewGuid(), Name = "Water", Sku = "WATER", CategoryId = category.Id, BaseUomId = unit.Id, Status = true };
        var otherProduct = new Product { Id = Guid.NewGuid(), Name = "Other", Sku = "OTHER", CategoryId = category.Id, BaseUomId = unit.Id, CompanyId = otherCompany.Id };
        db.Products.AddRange(product, otherProduct);
        await db.SaveChangesAsync();
        supplierId = supplier.Id; warehouseId = warehouse.Id; productId = product.Id; baseUomId = unit.Id; boxUomId = box.Id;
        foreignSupplierId = otherSupplier.Id; foreignWarehouseId = otherWarehouse.Id; foreignProductId = otherProduct.Id;
    }

    var create = NewOrder();
    var order = await Send(create);
    Check(order.Status == PurchaseOrderStatus.Pending && order.Items.Single().ReceivedQuantity == 0 && order.TotalAmount == 1000, "create computes pending, zero received, and total");
    Check(System.Text.RegularExpressions.Regex.IsMatch(order.PurchaseOrderNo, @"^PO-\d{4}-\d{8,}$"), "sequential number generated by server");
    identity.IsSuperAdmin = false;
    using (var scope = provider.CreateScope())
        Check(!await Db(scope).Suppliers.AnyAsync(s => s.Id == foreignSupplierId), "global company filter hides foreign supplier");
    identity.IsSuperAdmin = true;
    var replay = await Send(create);
    Check(replay.Id == order.Id, "same request replay returns original order");
    await Reject(create with { Items = [new(productId, 11, 100, baseUomId)] }, "changed payload cannot reuse key");
    await Reject(NewOrder() with { SupplierId = foreignSupplierId }, "foreign company supplier rejected");
    await Reject(NewOrder() with { WarehouseId = foreignWarehouseId }, "foreign company warehouse rejected");
    await Reject(NewOrder() with { Items = [new(foreignProductId, 10, 100, baseUomId)] }, "foreign company product rejected");
    await Reject(NewOrder() with { Items = [new(productId, 10, 100, boxUomId)] }, "missing base UOM conversion rejected");
    await Reject(NewOrder() with { IdempotencyKey = Guid.Empty }, "empty retry key rejected");
    await Reject(NewOrder() with { Items = [] }, "empty order rejected");
    await Reject(NewOrder() with { Items = [new(productId, 0.12345m, 100, baseUomId)] }, "quantity precision cannot silently round");
    await Reject(NewOrder() with { Items = [new(productId, 10, 100.001m, baseUomId)] }, "price precision cannot silently round");
    await Reject(NewOrder() with { Items = [new(productId, decimal.MaxValue, decimal.MaxValue, baseUomId)] }, "amount overflow is validation failure");

    using (var scope = provider.CreateScope())
    {
        Db(scope).ProductUomConversions.Add(new ProductUomConversion { ProductId = productId, FromUomId = boxUomId, ToUomId = baseUomId, ConversionFactor = 12, IsActive = true });
        await Db(scope).SaveChangesAsync();
    }
    var converted = await Send(NewOrder() with { Items = [new(productId, 10, 100, boxUomId)] });
    Check(converted.Items.Single().UomId == boxUomId, "active conversion permits purchase UOM");
    var edit = Edit(order, 10);
    await Reject(edit with { Items = [edit.Items[0], edit.Items[0]] }, "duplicate item ID rejected");
    await Reject(edit with { Items = [edit.Items[0] with { Id = converted.Items[0].Id }] }, "another PO's item ID rejected");
    var updated = await Send(edit with { Items = [edit.Items[0] with { Quantity = 12 }] });
    Check(updated!.TotalAmount == 1200, "update total follows active items");

    foreach (var state in new[] { "inactive", "deleted" })
    {
        using (var scope = provider.CreateScope())
        {
            var db = Db(scope);
            var supplier = await db.Suppliers.SingleAsync(s => s.Id == supplierId);
            var warehouse = await db.Warehouses.SingleAsync(w => w.Id == warehouseId);
            var product = await db.Products.SingleAsync(p => p.Id == productId);
            var uom = await db.UnitOfMeasures.SingleAsync(u => u.Id == baseUomId);
            supplier.Status = warehouse.Status = product.Status = uom.IsActive = state != "inactive";
            supplier.DeletedOn = warehouse.DeletedOn = product.DeletedOn = uom.DeletedOn = state == "deleted" ? DateTime.Now : null;
            await db.SaveChangesAsync();
        }
        await Reject(NewOrder(), $"{state} supplier rejected on create");
        await Reject(Edit(order, 12), $"{state} supplier rejected on update");
        using (var scope = provider.CreateScope())
        {
            var db = Db(scope);
            var supplier = await db.Suppliers.SingleAsync(s => s.Id == supplierId);
            supplier.Status = true; supplier.DeletedOn = null;
            await db.SaveChangesAsync();
        }
        await Reject(NewOrder(), $"{state} warehouse rejected on create");
        await Reject(Edit(order, 12), $"{state} warehouse rejected on update");
        using (var scope = provider.CreateScope())
        {
            var db = Db(scope);
            var warehouse = await db.Warehouses.SingleAsync(w => w.Id == warehouseId);
            warehouse.Status = true; warehouse.DeletedOn = null;
            await db.SaveChangesAsync();
        }
        await Reject(NewOrder(), $"{state} product rejected on create");
        await Reject(Edit(order, 12), $"{state} product rejected on update");
        using (var scope = provider.CreateScope())
        {
            var db = Db(scope);
            var product = await db.Products.SingleAsync(p => p.Id == productId);
            product.Status = true; product.DeletedOn = null;
            await db.SaveChangesAsync();
        }
        await Reject(NewOrder(), $"{state} UOM rejected on create");
        await Reject(Edit(order, 12), $"{state} UOM rejected on update");
        using (var scope = provider.CreateScope())
        {
            var uom = await Db(scope).UnitOfMeasures.SingleAsync(u => u.Id == baseUomId);
            uom.IsActive = true; uom.DeletedOn = null;
            await Db(scope).SaveChangesAsync();
        }
    }

    var receipt = new CreateGoodReceiptCommand("GR-TEST", warehouseId, order.Id, supplierId, DateTime.Today, true, "Tester", null, Guid.NewGuid(),
        [new(order.Items[0].Id, productId, baseUomId, 4)]);
    await Send(receipt);
    var afterReceipt = await Send(Edit(order, 12));
    Check(afterReceipt!.Items[0].ReceivedQuantity == 4 && afterReceipt.Status == PurchaseOrderStatus.PartiallyReceived, "stale PO edit preserves received quantity");
    await Reject(Edit(order, 3), "ordered quantity below received rejected");
    await Reject(Edit(order, 12) with { Items = [new(null, productId, 12, 100, baseUomId)] }, "received item removal rejected");
    await Reject(Edit(order, 12) with { Items = [edit.Items[0] with { UomId = boxUomId }] }, "received UOM change rejected");
    await Reject(new DeletePurchaseOrderCommand(order.Id), "PO with receipt cannot be deleted");
    await Reject(new CancelPurchaseOrderCommand(order.Id, "No longer needed"), "received PO cannot be cancelled");
    var complete = await Send(Edit(order, 4));
    Check(complete!.Status == PurchaseOrderStatus.Completed, "quantity reduction to received derives completed");
    var reopened = await Send(Edit(order, 12));
    Check(reopened!.Status == PurchaseOrderStatus.PartiallyReceived, "quantity increase derives partially received");
    var received = await Send(receipt);
    await Send(new DeleteGoodReceiptCommand(received.Id));
    using (var scope = provider.CreateScope())
    {
        var db = Db(scope);
        Check(await db.WarehouseStocks.Where(w => w.ProductId == productId).SumAsync(w => w.Quantity) == 0, "receipt reversal restores stock");
    }
    Check(await Send(new CancelPurchaseOrderCommand(converted.Id, "Cancelled by buyer")), "unreceived PO cancellation succeeds");
    await Reject(Edit(converted, 10), "cancelled PO cannot be edited");
    Check(await Send(new CancelPurchaseOrderCommand(converted.Id, "Repeated request")), "cancel retry is idempotent");

    // Concurrent creates may deadlock under serializable isolation. Retry the failed
    // request with the same key; committed operations must remain unique.
    var concurrent = NewOrder();
    async Task<PurchaseOrderDto> CreateWithRetry()
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return await Send(concurrent); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205 && attempt < 3) { await Task.Delay(100); }
        }
    }
    var results = await Task.WhenAll(CreateWithRetry(), CreateWithRetry());
    Check(results[0].Id == results[1].Id, "concurrent create retry yields one order");
    var distinctCreates = await Task.WhenAll(Send(NewOrder()), Send(NewOrder()));
    Check(distinctCreates[0].PurchaseOrderNo != distinctCreates[1].PurchaseOrderNo, "concurrent distinct creates receive distinct sequence numbers");
    using (var scope = provider.CreateScope())
    {
        Check(await Db(scope).PurchaseOrders.CountAsync(p => p.Id == concurrent.IdempotencyKey) == 1, "one persisted order per retry key");
    }
    var racingOrder = await Send(NewOrder());
    var racingReceipt = receipt with { PurchaseOrderId = racingOrder.Id, ReceiptNo = "GR-RACE", IdempotencyKey = Guid.NewGuid(),
        Items = [new(racingOrder.Items[0].Id, productId, baseUomId, 4)] };
    async Task<bool> TryOperation<T>(IRequest<T> request)
    {
        try { await Send(request); return true; }
        catch (InvalidOperationException) { return false; }
    }
    var race = await Task.WhenAll(TryOperation(Edit(racingOrder, 3)), TryOperation(racingReceipt));
    Check(race.Count(x => x) == 1, "concurrent quantity reduction and receipt cannot both commit");
    using (var scope = provider.CreateScope())
    {
        var item = await Db(scope).PurchaseOrderItems.SingleAsync(i => i.Id == racingOrder.Items[0].Id);
        Check(item.ReceivedQuantity <= item.Quantity, "concurrent receipt/edit preserves quantity invariant");
        Db(scope).PurchaseOrders.Add(new PurchaseOrder
        {
            Id = Guid.NewGuid(), CompanyId = identity.CompanyId, PurchaseOrderNo = order.PurchaseOrderNo,
            SupplierId = supplierId, WarehouseId = warehouseId, OrderDate = DateTime.Today, ExpectedDate = DateTime.Today
        });
        try { await Db(scope).SaveChangesAsync(); throw new Exception("Duplicate PO number accepted."); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
        { Check(true, "database rejects duplicate company PO number"); }
    }
    Console.WriteLine($"All {checks} purchase order integration checks passed.");
}
finally
{
    using var scope = provider.CreateScope();
    var db = Db(scope);
    // This context always uses the generated IMS_PO_Checks database above.
    if (db.Database.GetDbConnection().Database != database || !database.StartsWith("IMS_PO_Checks_"))
        throw new Exception("Refusing to clean up a non-test database.");
    await db.Database.EnsureDeletedAsync();
}

CreatePurchaseOrderCommand NewOrder() => new(supplierId, warehouseId, DateTime.Today, DateTime.Today.AddDays(7), [new(productId, 10, 100, baseUomId)], Guid.NewGuid());
UpdatePurchaseOrderCommand Edit(PurchaseOrderDto po, decimal quantity) => new(po.Id, supplierId, warehouseId, po.OrderDate, po.ExpectedDate,
    [new(po.Items[0].Id, productId, quantity, 100, po.Items[0].UomId)]);

class TestUser : ICurrentUserService
{
    public string? UserId => "po-test";
    public string? UserName => "PO test";
    public string? IpAddress => null;
    public int? CompanyId { get; set; }
    public bool IsSuperAdmin { get; set; } = true;
}

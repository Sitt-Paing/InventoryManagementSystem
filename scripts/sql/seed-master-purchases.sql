-- Receive the twelve additional master products using their purchase-pack conversions.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
DECLARE @Tag nvarchar(50)=N'DemoMastersOrders20261023',@User nvarchar(450)=(SELECT Id FROM dbo.AspNetUsers WHERE UserName='devadmin');
IF DB_NAME()<>'InventoryManagement' OR @User IS NULL THROW 51000,'Unexpected local database/admin.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource=@Tag,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @Lock<0 THROW 51000,'Unable to lock purchasing seed.',1;
IF EXISTS(SELECT 1 FROM dbo.PurchaseOrderItems i JOIN dbo.Products p ON p.Id=i.ProductId WHERE p.CompanyId=1 AND p.SKU LIKE 'M26-P%')
BEGIN
 COMMIT; SELECT N'Additional master purchases already exist; nothing added.' Result; RETURN;
END;
IF (SELECT COUNT(*) FROM dbo.Products WHERE CompanyId=1 AND SKU LIKE 'M26-P%' AND DeletedOn IS NULL)<>12
 THROW 51000,'Expected twelve master demo products.',1;
DECLARE @Orders TABLE(G int,Id uniqueidentifier,ReceiptId uniqueidentifier,Number bigint,Supplier int,Warehouse int,OrderDate datetime,ReceiptDate datetime);
INSERT @Orders
SELECT v.G,NEWID(),NEWID(),NEXT VALUE FOR dbo.PurchaseOrderNumberSequence,s.Id,w.Id,
 DATEADD(hour,9,DATEADD(day,v.G,'20261014')),DATEADD(hour,10,DATEADD(day,v.G,'20261017'))
FROM(VALUES(1),(2),(3),(4))v(G)
JOIN dbo.Suppliers s ON s.SupplierCode=CONCAT('M26-S0',v.G) AND s.CompanyId=1
JOIN dbo.Warehouses w ON w.WarehouseCode=CASE WHEN v.G IN(1,2) THEN 'M26-W01' ELSE 'M26-W02' END AND w.CompanyId=1;
IF (SELECT COUNT(*) FROM @Orders)<>4 THROW 51000,'Expected suppliers and warehouses were not found.',1;
INSERT dbo.PurchaseOrders(Id,CompanyId,PurchaseOrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,Status,TotalAmount,ApprovalStatus,ApprovedBy,ApprovedOn,CreatedOn,CreatedBy)
SELECT Id,1,CONCAT('PO-2026-',RIGHT('00000000'+CONVERT(varchar,Number),8)),Supplier,Warehouse,OrderDate,ReceiptDate,2,0,2,@User,DATEADD(hour,2,OrderDate),OrderDate,@Tag FROM @Orders;
INSERT dbo.PurchaseOrderItems(CompanyId,PurchaseOrderId,ProductId,Quantity,UnitPrice,UomId,ReceivedQuantity,CreatedOn,CreatedBy)
SELECT 1,o.Id,p.Id,v.Qty,p.CostPrice*c.ConversionFactor,p.PurchaseUomId,v.Qty,o.OrderDate,@Tag
FROM @Orders o JOIN dbo.Products p ON p.CompanyId=1 AND p.SKU LIKE 'M26-P%' AND (CONVERT(int,RIGHT(p.SKU,2))-1)/3+1=o.G
JOIN dbo.ProductUomConversion c ON c.ProductId=p.Id AND c.FromUomId=p.PurchaseUomId AND c.ToUomId=p.BaseUomId AND c.CompanyId=1
CROSS APPLY(SELECT CASE o.G WHEN 1 THEN 4 WHEN 2 THEN 3 WHEN 3 THEN 2 ELSE 5 END Qty)v;
UPDATE po SET TotalAmount=x.Amount FROM dbo.PurchaseOrders po JOIN
 (SELECT PurchaseOrderId,SUM(Quantity*UnitPrice) Amount FROM dbo.PurchaseOrderItems WHERE CreatedBy=@Tag GROUP BY PurchaseOrderId)x ON x.PurchaseOrderId=po.Id;
INSERT dbo.GoodReceipts(Id,CompanyId,ReceiptNo,WarehouseId,PurchaseOrderId,SupplierId,ReceiptDate,Status,TotalAmount,ReceivedBy,Note,CreatedOn,CreatedBy)
SELECT o.ReceiptId,1,CONCAT('GRN-',RIGHT('000000'+CONVERT(varchar,(DATEDIFF_BIG(millisecond,'19700101',o.ReceiptDate)+o.G*1879)%1000000),6)),
 o.Warehouse,o.Id,o.Supplier,o.ReceiptDate,1,p.TotalAmount,N'devadmin',N'Purchase packs counted; received quantity converted to base units.',o.ReceiptDate,@Tag
FROM @Orders o JOIN dbo.PurchaseOrders p ON p.Id=o.Id;
INSERT dbo.GoodReceiptItems(CompanyId,GoodReceiptId,PurchaseOrderItemId,ProductId,UomId,ReceivedQuantity,ReceivedBaseQuantity,UnitPrice,LineAmount,CreatedOn,CreatedBy)
SELECT 1,o.ReceiptId,i.Id,i.ProductId,i.UomId,i.Quantity,i.Quantity*c.ConversionFactor,i.UnitPrice,i.Quantity*i.UnitPrice,o.ReceiptDate,@Tag
FROM @Orders o JOIN dbo.PurchaseOrderItems i ON i.PurchaseOrderId=o.Id
JOIN dbo.Products p ON p.Id=i.ProductId JOIN dbo.ProductUomConversion c ON c.ProductId=p.Id AND c.FromUomId=i.UomId AND c.ToUomId=p.BaseUomId AND c.CompanyId=1;
INSERT dbo.StockTransactions(CompanyId,ProductId,UserId,WarehouseId,WarehouseLocationId,Quantity,TransactionType,TransactionDate,GoodReceiptId,GoodReceiptItemId,Note,CreatedOn,CreatedBy)
SELECT 1,i.ProductId,@User,o.Warehouse,0,i.ReceivedBaseQuantity,'IN',o.ReceiptDate,o.ReceiptId,i.Id,CONCAT('Goods Receipt: ',r.ReceiptNo),o.ReceiptDate,@Tag
FROM @Orders o JOIN dbo.GoodReceiptItems i ON i.GoodReceiptId=o.ReceiptId JOIN dbo.GoodReceipts r ON r.Id=o.ReceiptId;
INSERT dbo.WarehouseStocks(CompanyId,ProductId,WarehouseId,Quantity,CreatedOn,CreatedBy)
SELECT 1,i.ProductId,o.Warehouse,i.ReceivedBaseQuantity,o.ReceiptDate,@Tag FROM @Orders o JOIN dbo.GoodReceiptItems i ON i.GoodReceiptId=o.ReceiptId;
UPDATE p SET CurrentStock=x.Qty,UpdatedBy=@Tag,UpdatedOn='20261023' FROM dbo.Products p JOIN
 (SELECT ProductId,SUM(Quantity) Qty FROM dbo.WarehouseStocks WHERE CreatedBy=@Tag GROUP BY ProductId)x ON x.ProductId=p.Id;
IF (SELECT COUNT(*) FROM dbo.GoodReceiptItems WHERE CreatedBy=@Tag)<>12 OR
 (SELECT COUNT(*) FROM dbo.StockTransactions WHERE CreatedBy=@Tag)<>12 THROW 51000,'Incomplete master purchasing batch.',1;
COMMIT;
SELECT N'Added 4 completed purchase orders, 4 goods receipts, and 12 stock receipt lines.' Result;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;

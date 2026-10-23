-- Read-only reconciliation, scoped by saved row IDs instead of audit usernames.
SET NOCOUNT ON;
IF OBJECT_ID('tempdb..#DemoRecords') IS NULL THROW 51000,'Run through Invoke-LocalInventorySql.ps1 to load the manifest.',1;
DECLARE @Checks TABLE(CheckName nvarchar(100),Failures int);
DECLARE @Table sysname,@Sql nvarchar(max),@Actual int,@Expected int;
DECLARE audits CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT TableName FROM #DemoRecords;
OPEN audits; FETCH NEXT FROM audits INTO @Table;
WHILE @@FETCH_STATUS=0
BEGIN
 IF @Table NOT IN('Categories','Suppliers','UOM_Category','UnitOfMeasure','Warehouses','Warehouse_Locations','Department','Products','ProductUomConversion',
 'PurchaseOrders','PurchaseOrderItems','GoodReceipts','GoodReceiptItems','PurchaseReturns','PurchaseReturnItems','StockIssues','StockIssueItems','StockTransactions','WarehouseStocks') THROW 51000,'Unexpected manifest table.',1;
 SELECT @Expected=COUNT(*) FROM #DemoRecords WHERE TableName=@Table;
 SET @Sql=N'SELECT @Actual=COUNT(*) FROM dbo.'+QUOTENAME(@Table)+N' t JOIN #DemoRecords m ON m.TableName=@Table AND m.Id=CONVERT(nvarchar(50),t.Id) WHERE t.CompanyId=1 AND t.CreatedBy=N''devadmin'' AND t.UpdatedBy=N''devadmin'';';
 EXEC sys.sp_executesql @Sql,N'@Table sysname,@Actual int OUTPUT',@Table,@Actual OUTPUT;
 INSERT @Checks VALUES(CONCAT(@Table,': row count, company and audit names'),ABS(@Expected-@Actual));
 FETCH NEXT FROM audits INTO @Table;
END;
CLOSE audits; DEALLOCATE audits;
SELECT p.* INTO #P FROM dbo.Products p JOIN #DemoRecords m ON m.TableName='Products' AND m.Id=p.Id;
SELECT s.* INTO #WS FROM dbo.WarehouseStocks s JOIN #DemoRecords m ON m.TableName='WarehouseStocks' AND m.Id=CONVERT(nvarchar(50),s.Id);
SELECT s.* INTO #ST FROM dbo.StockTransactions s JOIN #DemoRecords m ON m.TableName='StockTransactions' AND m.Id=CONVERT(nvarchar(50),s.Id);
SELECT s.* INTO #PO FROM dbo.PurchaseOrders s JOIN #DemoRecords m ON m.TableName='PurchaseOrders' AND m.Id=CONVERT(nvarchar(50),s.Id);
SELECT s.* INTO #GR FROM dbo.GoodReceipts s JOIN #DemoRecords m ON m.TableName='GoodReceipts' AND m.Id=CONVERT(nvarchar(50),s.Id);
SELECT s.* INTO #PR FROM dbo.PurchaseReturns s JOIN #DemoRecords m ON m.TableName='PurchaseReturns' AND m.Id=CONVERT(nvarchar(50),s.Id);
SELECT s.* INTO #SI FROM dbo.StockIssues s JOIN #DemoRecords m ON m.TableName='StockIssues' AND m.Id=CONVERT(nvarchar(50),s.Id);
IF (SELECT COUNT(*) FROM #P)<>42 OR (SELECT COUNT(*) FROM #PO)<>46 OR (SELECT COUNT(*) FROM #GR)<>34 THROW 51000,'Incomplete manifest batch.',1;
INSERT @Checks SELECT 'Product totals equal warehouse balances',COUNT(*) FROM #P p OUTER APPLY(SELECT SUM(Quantity) Qty FROM dbo.WarehouseStocks WHERE ProductId=p.Id AND CompanyId=1 AND DeletedOn IS NULL)x WHERE p.CurrentStock<>COALESCE(x.Qty,0);
INSERT @Checks SELECT 'Warehouse balances equal stock ledger',COUNT(*) FROM #WS s OUTER APPLY(SELECT SUM(CASE TransactionType WHEN 'IN' THEN Quantity ELSE -Quantity END) Qty FROM dbo.StockTransactions WHERE ProductId=s.ProductId AND WarehouseId=s.WarehouseId AND CompanyId=1 AND DeletedOn IS NULL)x WHERE s.Quantity<>COALESCE(x.Qty,0);
INSERT @Checks SELECT 'PO amounts equal line amounts',COUNT(*) FROM #PO p OUTER APPLY(SELECT SUM(Quantity*UnitPrice) Amount FROM dbo.PurchaseOrderItems WHERE PurchaseOrderId=p.Id AND DeletedOn IS NULL)x WHERE p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks SELECT 'Master creation, approval, order and receipt dates agree',COUNT(*) FROM #GR g JOIN #PO po ON po.Id=g.PurchaseOrderId
JOIN dbo.Suppliers s ON s.Id=po.SupplierId JOIN dbo.Warehouses w ON w.Id=po.WarehouseId
WHERE g.ReceiptDate<po.OrderDate OR po.ApprovedOn<po.OrderDate OR po.ApprovedOn>g.ReceiptDate OR s.CreatedOn>po.OrderDate OR w.CreatedOn>po.OrderDate
OR EXISTS(SELECT 1 FROM dbo.PurchaseOrderItems i JOIN dbo.Products p ON p.Id=i.ProductId WHERE i.PurchaseOrderId=po.Id AND p.CreatedOn>po.OrderDate);
INSERT @Checks SELECT 'GRN amounts equal line amounts',COUNT(*) FROM #GR p OUTER APPLY(SELECT SUM(LineAmount) Amount FROM dbo.GoodReceiptItems WHERE GoodReceiptId=p.Id AND DeletedOn IS NULL)x WHERE p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks SELECT 'Return amounts equal line amounts',COUNT(*) FROM #PR p OUTER APPLY(SELECT SUM(ReturnQuantity*UnitPrice) Amount FROM dbo.PurchaseReturnItems WHERE PurchaseReturnId=p.Id AND DeletedOn IS NULL)x WHERE p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks SELECT 'PO received quantities equal receipt lines',COUNT(*) FROM dbo.PurchaseOrderItems i JOIN #PO p ON p.Id=i.PurchaseOrderId
OUTER APPLY(SELECT SUM(ReceivedQuantity) Qty FROM dbo.GoodReceiptItems WHERE PurchaseOrderItemId=i.Id AND DeletedOn IS NULL)x WHERE i.ReceivedQuantity<>COALESCE(x.Qty,0) OR i.ReceivedQuantity>i.Quantity;
INSERT @Checks SELECT 'Receipt pack conversions and PO links agree',COUNT(*) FROM dbo.GoodReceiptItems i JOIN #GR g ON g.Id=i.GoodReceiptId
JOIN dbo.PurchaseOrderItems oi ON oi.Id=i.PurchaseOrderItemId JOIN dbo.Products p ON p.Id=i.ProductId JOIN dbo.PurchaseOrders po ON po.Id=g.PurchaseOrderId
OUTER APPLY(SELECT MAX(ConversionFactor) Factor FROM dbo.ProductUomConversion WHERE ProductId=i.ProductId AND FromUomId=i.UomId AND ToUomId=p.BaseUomId AND CompanyId=1)c
WHERE oi.PurchaseOrderId<>g.PurchaseOrderId OR oi.ProductId<>i.ProductId OR oi.UomId<>i.UomId OR g.SupplierId<>po.SupplierId OR g.WarehouseId<>po.WarehouseId OR po.ApprovalStatus<>2
 OR i.LineAmount<>i.ReceivedQuantity*i.UnitPrice OR i.ReceivedBaseQuantity<>i.ReceivedQuantity*CASE WHEN i.UomId=p.BaseUomId THEN 1 ELSE COALESCE(c.Factor,0) END;
INSERT @Checks SELECT 'Each receipt line has one matching IN movement',COUNT(*) FROM dbo.GoodReceiptItems i JOIN #GR g ON g.Id=i.GoodReceiptId
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM #ST t WHERE t.GoodReceiptId=g.Id AND t.GoodReceiptItemId=i.Id AND t.ProductId=i.ProductId AND t.WarehouseId=g.WarehouseId AND t.TransactionType='IN')x WHERE x.C<>1 OR x.Qty<>i.ReceivedBaseQuantity;
INSERT @Checks SELECT 'Posted issues match OUT; drafts do not affect stock',COUNT(*) FROM dbo.StockIssueItems i JOIN #SI s ON s.Id=i.StockIssueId
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM #ST t WHERE t.StockIssueId=s.Id AND t.StockIssueItemId=i.Id AND t.ProductId=i.ProductId AND t.WarehouseId=s.WarehouseId AND t.TransactionType='OUT')x
WHERE (s.Status=1 AND (x.C<>1 OR x.Qty<>i.Quantity)) OR (s.Status=0 AND x.C<>0);
INSERT @Checks SELECT 'Posted returns match OUT; drafts do not affect stock',COUNT(*) FROM dbo.PurchaseReturnItems i JOIN #PR r ON r.Id=i.PurchaseReturnId
JOIN dbo.GoodReceiptItems gi ON gi.Id=i.GoodReceiptItemId JOIN dbo.GoodReceipts gr ON gr.Id=gi.GoodReceiptId
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM #ST t WHERE t.PurchaseReturnId=r.Id AND t.PurchaseReturnItemId=i.Id AND t.ProductId=gi.ProductId AND t.WarehouseId=gr.WarehouseId AND t.TransactionType='OUT')x
WHERE (r.Status=1 AND (x.C<>1 OR x.Qty<>i.ReturnQuantity)) OR (r.Status=0 AND x.C<>0);
INSERT @Checks SELECT 'No historical negative stock',COUNT(*) FROM(SELECT SUM(CASE TransactionType WHEN 'IN' THEN Quantity ELSE -Quantity END) OVER(PARTITION BY ProductId,WarehouseId ORDER BY TransactionDate,Id ROWS UNBOUNDED PRECEDING) Balance FROM #ST)x WHERE Balance<0;
INSERT @Checks SELECT 'PO, GRN and return numbers match default formats',COUNT(*) FROM(
 SELECT PurchaseOrderNo Number,CONCAT('PO-',YEAR(OrderDate),'-') Prefix,8 Digits FROM #PO UNION ALL SELECT ReceiptNo,'GRN-',6 FROM #GR UNION ALL SELECT ReturnNo,CONCAT('PR-',YEAR(ReturnDate),'-'),8 FROM #PR
)x WHERE LEFT(Number,LEN(Prefix))<>Prefix OR LEN(Number)<>LEN(Prefix)+Digits OR SUBSTRING(Number,LEN(Prefix)+1,50) LIKE '%[^0-9]%';
INSERT @Checks SELECT 'Issue numbers match SI-year-eight-hex format',COUNT(*) FROM #SI WHERE LEFT(IssueNo,8)<>CONCAT('SI-',YEAR(IssueDate),'-') OR LEN(IssueNo)<>16 OR RIGHT(IssueNo,8) COLLATE Latin1_General_BIN2 LIKE '%[^0-9A-F]%';
INSERT @Checks SELECT 'Stock notes reference current document numbers',COUNT(*) FROM #ST t LEFT JOIN #GR g ON g.Id=t.GoodReceiptId LEFT JOIN #PR r ON r.Id=t.PurchaseReturnId LEFT JOIN #SI s ON s.Id=t.StockIssueId
WHERE t.Note<>CASE WHEN g.Id IS NOT NULL THEN CONCAT('Goods Receipt: ',g.ReceiptNo) WHEN r.Id IS NOT NULL THEN CONCAT('Purchase Return: ',r.ReturnNo) ELSE CONCAT('Stock Issue: ',s.IssueNo) END;
SELECT CheckName,Failures FROM @Checks;
IF EXISTS(SELECT 1 FROM @Checks WHERE Failures<>0) THROW 51000,'Normalized demo reconciliation failed.',1;
SELECT 'Products' Entity,COUNT(*) Records FROM #P UNION ALL SELECT 'Purchase orders',COUNT(*) FROM #PO UNION ALL SELECT 'Goods receipts',COUNT(*) FROM #GR UNION ALL SELECT 'Purchase returns',COUNT(*) FROM #PR UNION ALL SELECT 'Stock issues',COUNT(*) FROM #SI UNION ALL SELECT 'Stock movements',COUNT(*) FROM #ST;

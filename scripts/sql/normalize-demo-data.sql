-- Match the application's default document formats and real audit user.
-- Run through Invoke-LocalInventorySql.ps1, which loads the immutable local row manifest.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF DB_NAME()<>'InventoryManagement' OR OBJECT_ID('tempdb..#DemoRecords') IS NULL
 THROW 51000,'Expected local database and demo row manifest.',1;
DECLARE @Now datetime=DATEADD(minute,390,GETUTCDATE()),@Lock int;
IF (SELECT COUNT(*) FROM #DemoRecords WHERE TableName='PurchaseOrders')<>46
 OR (SELECT COUNT(*) FROM #DemoRecords WHERE TableName='Products')<>42
 THROW 51000,'Unexpected demo manifest.',1;
BEGIN TRY
BEGIN TRANSACTION;
EXEC @Lock=sys.sp_getapplock @Resource=N'NormalizeInventoryDemo20261023',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @Lock<0 THROW 51000,'Unable to lock normalization.',1;

DECLARE @Balances TABLE(T nvarchar(30),Id nvarchar(50),Quantity decimal(18,4));
INSERT @Balances SELECT 'Products',p.Id,p.CurrentStock FROM dbo.Products p JOIN #DemoRecords m ON m.TableName='Products' AND m.Id=p.Id WHERE p.CompanyId=1;
INSERT @Balances SELECT 'WarehouseStocks',CONVERT(nvarchar(50),p.Id),p.Quantity FROM dbo.WarehouseStocks p JOIN #DemoRecords m ON m.TableName='WarehouseStocks' AND m.Id=CONVERT(nvarchar(50),p.Id) WHERE p.CompanyId=1;
INSERT @Balances SELECT 'StockTransactions',CONVERT(nvarchar(50),p.Id),p.Quantity FROM dbo.StockTransactions p JOIN #DemoRecords m ON m.TableName='StockTransactions' AND m.Id=CONVERT(nvarchar(50),p.Id) WHERE p.CompanyId=1;

DECLARE @Id uniqueidentifier,@Number nvarchar(50),@Sequence bigint,@Year int,@Date datetime,@Old nvarchar(50);
DECLARE po_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT p.Id,YEAR(p.OrderDate) FROM dbo.PurchaseOrders p WITH(UPDLOCK,HOLDLOCK)
 JOIN #DemoRecords m ON m.TableName='PurchaseOrders' AND m.Id=CONVERT(nvarchar(50),p.Id)
 WHERE p.CompanyId=1 AND p.PurchaseOrderNo LIKE 'DEMO-%' ORDER BY p.OrderDate,p.Id;
OPEN po_cursor;
FETCH NEXT FROM po_cursor INTO @Id,@Year;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @Sequence=NEXT VALUE FOR dbo.PurchaseOrderNumberSequence;
 SET @Number=CONCAT('PO-',@Year,'-',RIGHT('00000000'+CONVERT(varchar,@Sequence),8));
 WHILE EXISTS(SELECT 1 FROM dbo.PurchaseOrders WHERE PurchaseOrderNo=@Number)
 BEGIN
  SET @Sequence=NEXT VALUE FOR dbo.PurchaseOrderNumberSequence;
  SET @Number=CONCAT('PO-',@Year,'-',RIGHT('00000000'+CONVERT(varchar,@Sequence),8));
 END;
 UPDATE dbo.PurchaseOrders SET PurchaseOrderNo=@Number WHERE Id=@Id AND CompanyId=1;
 FETCH NEXT FROM po_cursor INTO @Id,@Year;
END;
CLOSE po_cursor; DEALLOCATE po_cursor;

DECLARE pr_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT p.Id,YEAR(p.ReturnDate) FROM dbo.PurchaseReturns p WITH(UPDLOCK,HOLDLOCK)
 JOIN #DemoRecords m ON m.TableName='PurchaseReturns' AND m.Id=CONVERT(nvarchar(50),p.Id)
 WHERE p.CompanyId=1 AND p.ReturnNo LIKE 'DEMO-%' ORDER BY p.ReturnDate,p.Id;
OPEN pr_cursor;
FETCH NEXT FROM pr_cursor INTO @Id,@Year;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @Sequence=COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING(ReturnNo,9,50))),0)+1
 FROM dbo.PurchaseReturns WITH(UPDLOCK,HOLDLOCK) WHERE CompanyId=1 AND ReturnNo LIKE CONCAT('PR-',@Year,'-%');
 SET @Number=CONCAT('PR-',@Year,'-',RIGHT('00000000'+CONVERT(varchar,@Sequence),8));
 UPDATE dbo.PurchaseReturns SET ReturnNo=@Number WHERE Id=@Id AND CompanyId=1;
 FETCH NEXT FROM pr_cursor INTO @Id,@Year;
END;
CLOSE pr_cursor; DEALLOCATE pr_cursor;

DECLARE gr_cursor CURSOR LOCAL FAST_FORWARD FOR
 SELECT p.Id,p.ReceiptDate FROM dbo.GoodReceipts p WITH(UPDLOCK,HOLDLOCK)
 JOIN #DemoRecords m ON m.TableName='GoodReceipts' AND m.Id=CONVERT(nvarchar(50),p.Id)
 WHERE p.CompanyId=1 AND p.ReceiptNo LIKE 'DEMO-%' ORDER BY p.ReceiptDate,p.Id;
DECLARE @Counter bigint=0;
OPEN gr_cursor;
FETCH NEXT FROM gr_cursor INTO @Id,@Date;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @Counter=@Counter+1373;
 -- Same six-digit millisecond suffix as the GRN form's default generator.
 SET @Sequence=(DATEDIFF_BIG(millisecond,'19700101',@Date)+28800000+@Counter)%1000000;
 SET @Number=CONCAT('GRN-',RIGHT('000000'+CONVERT(varchar,@Sequence),6));
 WHILE EXISTS(SELECT 1 FROM dbo.GoodReceipts WHERE ReceiptNo=@Number AND Id<>@Id)
 BEGIN
  SET @Sequence=(@Sequence+1)%1000000;
  SET @Number=CONCAT('GRN-',RIGHT('000000'+CONVERT(varchar,@Sequence),6));
 END;
 UPDATE dbo.GoodReceipts SET ReceiptNo=@Number WHERE Id=@Id AND CompanyId=1;
 FETCH NEXT FROM gr_cursor INTO @Id,@Date;
END;
CLOSE gr_cursor; DEALLOCATE gr_cursor;

IF EXISTS(SELECT 1 FROM dbo.StockIssues p JOIN #DemoRecords m ON m.TableName='StockIssues' AND m.Id=CONVERT(nvarchar(50),p.Id)
 JOIN dbo.StockIssues other ON other.IssueNo=CONCAT('SI-',YEAR(p.IssueDate),'-',UPPER(LEFT(CONVERT(varchar(36),p.Id),8))) AND other.Id<>p.Id
 WHERE p.CompanyId=1) THROW 51000,'Issue number collision.',1;
UPDATE p SET IssueNo=CONCAT('SI-',YEAR(p.IssueDate),'-',UPPER(LEFT(CONVERT(varchar(36),p.Id),8)))
FROM dbo.StockIssues p JOIN #DemoRecords m ON m.TableName='StockIssues' AND m.Id=CONVERT(nvarchar(50),p.Id)
WHERE p.CompanyId=1 AND p.IssueNo LIKE 'DEMO-%';

UPDATE t SET Note=CASE WHEN t.GoodReceiptId IS NOT NULL THEN CONCAT('Goods Receipt: ',gr.ReceiptNo)
 WHEN t.PurchaseReturnId IS NOT NULL THEN CONCAT('Purchase Return: ',pr.ReturnNo)
 WHEN t.StockIssueId IS NOT NULL THEN CONCAT('Stock Issue: ',si.IssueNo) ELSE t.Note END
FROM dbo.StockTransactions t JOIN #DemoRecords m ON m.TableName='StockTransactions' AND m.Id=CONVERT(nvarchar(50),t.Id)
LEFT JOIN dbo.GoodReceipts gr ON gr.Id=t.GoodReceiptId LEFT JOIN dbo.PurchaseReturns pr ON pr.Id=t.PurchaseReturnId LEFT JOIN dbo.StockIssues si ON si.Id=t.StockIssueId
WHERE t.CompanyId=1;

-- Only manifest rows are edited. Keep their source outside CreatedBy/UpdatedBy.
DECLARE @Table sysname,@Sql nvarchar(max),@Expected int,@Actual int;
DECLARE audit_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT TableName FROM #DemoRecords;
OPEN audit_cursor; FETCH NEXT FROM audit_cursor INTO @Table;
WHILE @@FETCH_STATUS=0
BEGIN
 IF @Table NOT IN('Categories','Suppliers','UOM_Category','UnitOfMeasure','Warehouses','Warehouse_Locations','Department','Products','ProductUomConversion',
 'PurchaseOrders','PurchaseOrderItems','GoodReceipts','GoodReceiptItems','PurchaseReturns','PurchaseReturnItems','StockIssues','StockIssueItems','StockTransactions','WarehouseStocks')
 THROW 51000,'Unexpected manifest table.',1;
 SELECT @Expected=COUNT(*) FROM #DemoRecords WHERE TableName=@Table;
 SET @Sql=N'UPDATE t SET CreatedBy=N''devadmin'',UpdatedBy=N''devadmin'',UpdatedOn=@Now,
 CreatedOn=CASE WHEN m.Batch=N''DemoMasters20261023'' AND t.CreatedOn>CONVERT(datetime,''20261014'') THEN CONVERT(datetime,''20261014'') ELSE t.CreatedOn END
 FROM dbo.'+QUOTENAME(@Table)+N' t
 JOIN #DemoRecords m ON m.TableName=@Table AND m.Id=CONVERT(nvarchar(50),t.Id) WHERE t.CompanyId=1; SET @Actual=@@ROWCOUNT;';
 EXEC sys.sp_executesql @Sql,N'@Now datetime,@Table sysname,@Actual int OUTPUT',@Now,@Table,@Actual OUTPUT;
 IF @Actual<>@Expected THROW 51000,'Manifest row scope mismatch; normalization rolled back.',1;
 FETCH NEXT FROM audit_cursor INTO @Table;
END;
CLOSE audit_cursor; DEALLOCATE audit_cursor;
IF EXISTS(SELECT 1 FROM @Balances b JOIN dbo.Products p ON b.T='Products' AND b.Id=p.Id WHERE b.Quantity<>p.CurrentStock)
 OR EXISTS(SELECT 1 FROM @Balances b JOIN dbo.WarehouseStocks s ON b.T='WarehouseStocks' AND b.Id=CONVERT(nvarchar(50),s.Id) WHERE b.Quantity<>s.Quantity)
 OR EXISTS(SELECT 1 FROM @Balances b JOIN dbo.StockTransactions s ON b.T='StockTransactions' AND b.Id=CONVERT(nvarchar(50),s.Id) WHERE b.Quantity<>s.Quantity)
 THROW 51000,'Stock quantities changed unexpectedly.',1;
COMMIT;
SELECT N'Normalized document numbers and audit names; stock quantities preserved.' Result;
SELECT m.TableName,COUNT(*) Records FROM #DemoRecords m GROUP BY m.TableName ORDER BY m.TableName;
SELECT TOP 4 PurchaseOrderNo,CreatedBy,UpdatedBy FROM dbo.PurchaseOrders WHERE CompanyId=1 AND Id IN(SELECT TRY_CONVERT(uniqueidentifier,Id) FROM #DemoRecords WHERE TableName='PurchaseOrders') ORDER BY PurchaseOrderNo;
SELECT TOP 4 ReceiptNo,CreatedBy,UpdatedBy FROM dbo.GoodReceipts WHERE CompanyId=1 AND Id IN(SELECT TRY_CONVERT(uniqueidentifier,Id) FROM #DemoRecords WHERE TableName='GoodReceipts') ORDER BY ReceiptNo;
SELECT TOP 4 ReturnNo,CreatedBy,UpdatedBy FROM dbo.PurchaseReturns WHERE CompanyId=1 AND Id IN(SELECT TRY_CONVERT(uniqueidentifier,Id) FROM #DemoRecords WHERE TableName='PurchaseReturns') ORDER BY ReturnNo;
SELECT TOP 4 IssueNo,CreatedBy,UpdatedBy FROM dbo.StockIssues WHERE CompanyId=1 AND Id IN(SELECT TRY_CONVERT(uniqueidentifier,Id) FROM #DemoRecords WHERE TableName='StockIssues') ORDER BY IssueNo;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;

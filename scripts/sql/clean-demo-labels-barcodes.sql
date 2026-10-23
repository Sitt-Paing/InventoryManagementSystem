-- Only rows in the saved seed manifest are edited; accounts and balances are preserved.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME()<>'InventoryManagement' OR OBJECT_ID('tempdb..#DemoRecords') IS NULL THROW 51000,'Expected local database and row manifest.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Now datetime=DATEADD(minute,390,GETUTCDATE());
DECLARE @Fields TABLE(TableName sysname,ColumnName sysname);
INSERT @Fields VALUES('Categories','Name'),('Suppliers','CompanyName'),('Warehouses','Name'),('Department','DeptName'),('UOM_Category','Name'),('UnitOfMeasure','Name'),('Products','Name');
DECLARE @Table sysname,@Column sysname,@Sql nvarchar(max);
DECLARE labels CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,ColumnName FROM @Fields;
OPEN labels; FETCH NEXT FROM labels INTO @Table,@Column;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @Sql=N'UPDATE t SET '+QUOTENAME(@Column)+N'=LTRIM(RTRIM(REPLACE('+QUOTENAME(@Column)+N',N''(Demo)'',N''''))),UpdatedBy=N''devadmin'',UpdatedOn=@Now
 FROM dbo.'+QUOTENAME(@Table)+N' t JOIN #DemoRecords m ON m.TableName=@Table AND m.Id=CONVERT(nvarchar(50),t.Id)
 WHERE t.CompanyId=1 AND '+QUOTENAME(@Column)+N' LIKE N''%(Demo)%'';';
 EXEC sys.sp_executesql @Sql,N'@Table sysname,@Now datetime',@Table,@Now;
 FETCH NEXT FROM labels INTO @Table,@Column;
END;
CLOSE labels; DEALLOCATE labels;
UPDATE l SET Barcode=CONCAT('LOC-',l.LocationCode),UpdatedBy='devadmin',UpdatedOn=@Now
FROM dbo.Warehouse_Locations l JOIN #DemoRecords m ON m.TableName='Warehouse_Locations' AND m.Id=CONVERT(nvarchar(50),l.Id)
WHERE l.CompanyId=1 AND NULLIF(LTRIM(RTRIM(l.Barcode)),'') IS NULL;
UPDATE c SET Barcode=CONCAT('PACK-',p.SKU),UpdatedBy='devadmin',UpdatedOn=@Now
FROM dbo.ProductUomConversion c JOIN #DemoRecords m ON m.TableName='ProductUomConversion' AND m.Id=CONVERT(nvarchar(50),c.Id)
JOIN dbo.Products p ON p.Id=c.ProductId AND p.CompanyId=c.CompanyId
WHERE c.CompanyId=1 AND NULLIF(LTRIM(RTRIM(c.Barcode)),'') IS NULL;
UPDATE p SET Barcode=CONCAT('ITEM-',p.SKU),UpdatedBy='devadmin',UpdatedOn=@Now
FROM dbo.Products p JOIN #DemoRecords m ON m.TableName='Products' AND m.Id=p.Id
WHERE p.CompanyId=1 AND NULLIF(LTRIM(RTRIM(p.Barcode)),'') IS NULL;
IF EXISTS(SELECT Barcode FROM (
 SELECT Barcode FROM dbo.Products WHERE CompanyId=1 AND DeletedOn IS NULL
 UNION ALL SELECT Barcode FROM dbo.Warehouse_Locations WHERE CompanyId=1 AND DeletedOn IS NULL
 UNION ALL SELECT Barcode FROM dbo.ProductUomConversion WHERE CompanyId=1 AND DeletedOn IS NULL
)b WHERE NULLIF(Barcode,'') IS NOT NULL GROUP BY Barcode HAVING COUNT(*)>1)
 THROW 51000,'Duplicate barcode detected; changes rolled back.',1;
COMMIT;
SELECT N'Removed (Demo) names and completed product, location and packaging barcodes.' Result;
SELECT 'Warehouse locations' Entity,COUNT(*) Records,SUM(CASE WHEN NULLIF(Barcode,'') IS NULL THEN 1 ELSE 0 END) MissingBarcodes
FROM dbo.Warehouse_Locations l JOIN #DemoRecords m ON m.TableName='Warehouse_Locations' AND m.Id=CONVERT(nvarchar(50),l.Id)
UNION ALL SELECT 'Packaging conversions',COUNT(*),SUM(CASE WHEN NULLIF(Barcode,'') IS NULL THEN 1 ELSE 0 END)
FROM dbo.ProductUomConversion c JOIN #DemoRecords m ON m.TableName='ProductUomConversion' AND m.Id=CONVERT(nvarchar(50),c.Id)
UNION ALL SELECT 'Products',COUNT(*),SUM(CASE WHEN NULLIF(Barcode,'') IS NULL THEN 1 ELSE 0 END)
FROM dbo.Products p JOIN #DemoRecords m ON m.TableName='Products' AND m.Id=p.Id;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;

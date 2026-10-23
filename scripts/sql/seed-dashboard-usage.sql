-- Explicit, one-time visualization data requested for the local InventoryManagement DB.
-- Adds 10 posted issue items (Rice: 65 PCS, TV: 12 PCS) with matching OUT ledger
-- entries and stock deductions. Never run this automatically during application startup.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @CompanyId int = 1;
DECLARE @WarehouseId int = 1;
DECLARE @UserName nvarchar(256) = N'devadmin';
DECLARE @UserId nvarchar(450) = (SELECT Id FROM dbo.AspNetUsers WHERE UserName = @UserName);
DECLARE @Now datetime = GETDATE();
DECLARE @Rice nvarchar(36) = N'b0ed224c-9449-4ccd-82c8-1140b4bc7f60';
DECLARE @TV nvarchar(36) = N'5c20609d-8baf-4a03-906f-f2bf3a8179d1';

IF DB_NAME() <> N'InventoryManagement' THROW 51000, 'Unexpected database; seed aborted.', 1;
IF @UserId IS NULL THROW 51000, 'Expected local administrator was not found.', 1;
IF (SELECT COUNT(*) FROM dbo.Products WHERE CompanyId=@CompanyId AND Id IN (@Rice,@TV)
    AND Status=1 AND DeletedOn IS NULL) <> 2
    THROW 51000, 'Expected active company products were not found.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Id=@WarehouseId AND CompanyId=@CompanyId AND Status=1 AND DeletedOn IS NULL)
    THROW 51000, 'Expected active warehouse was not found.', 1;
IF (SELECT COUNT(*) FROM dbo.Department WHERE CompanyId=@CompanyId AND Id IN (1,2) AND IsActive=1 AND DeletedOn IS NULL) <> 2
    THROW 51000, 'Expected active departments were not found.', 1;

DECLARE @Rows TABLE (Number int PRIMARY KEY, IssueId uniqueidentifier, ProductId nvarchar(36),
    IssueDate datetime, Quantity decimal(18,4), DepartmentId int);
INSERT INTO @Rows VALUES
    (1,'183bc14c-fdc4-4ef9-9aa6-edfdbb354c34',@Rice,'20261014',10,2),
    (2,'d98f1ee0-feac-49d5-922f-2f72c0d4f999',@TV,'20261015',2,1),
    (3,'0d4291e9-0e72-4981-a694-da454f053081',@Rice,'20261016',15,2),
    (4,'bc4f1426-3ba2-4b84-a4c6-8a42aeab2da2',@TV,'20261017',1,1),
    (5,'3b7569c6-2dea-4c85-9457-f51440e3c8ed',@Rice,'20261018',8,2),
    (6,'7df8929f-b571-44e9-b38f-a483e706193a',@TV,'20261019',3,1),
    (7,'f69f1e54-4a3f-4ca4-8eb8-0ea2127cf011',@Rice,'20261020',20,2),
    (8,'7211132f-fae5-47f0-b28a-44b15bf60b8e',@TV,'20261021',2,1),
    (9,'6acc15a4-b660-43ff-9461-ce899d961294',@Rice,'20261022',12,2),
    (10,'00544e3c-ad11-43c4-9a62-b81437c44734',@TV,'20261023',4,1);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult = sys.sp_getapplock @Resource=N'DashboardVisualizationSeed20261023',
        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
    IF @LockResult < 0 THROW 51000, 'Unable to lock the seed batch.', 1;

    -- Stable IDs preserve idempotency even when issue numbers follow the normal SI format.
    DECLARE @Existing int = (SELECT COUNT(*) FROM dbo.StockIssues WHERE CompanyId=@CompanyId AND Id IN (SELECT IssueId FROM @Rows));
    IF @Existing > 0
    BEGIN
        IF @Existing <> 10 THROW 51000, 'An incomplete seed batch already exists; review it before retrying.', 1;
        COMMIT;
        SELECT N'Already present; no additional stock deductions.' AS Result, @Existing AS SeedIssueCount;
        RETURN;
    END;

    DECLARE @Required TABLE (ProductId nvarchar(36) PRIMARY KEY, Quantity decimal(18,4));
    INSERT INTO @Required SELECT ProductId,SUM(Quantity) FROM @Rows GROUP BY ProductId;
    IF (SELECT COUNT(*) FROM dbo.WarehouseStocks WITH (UPDLOCK,HOLDLOCK)
        WHERE CompanyId=@CompanyId AND WarehouseId=@WarehouseId AND ProductId IN (@Rice,@TV) AND DeletedOn IS NULL) <> 2
        THROW 51000, 'Expected unique warehouse stock rows were not found.', 1;
    IF EXISTS (SELECT 1 FROM @Required r JOIN dbo.WarehouseStocks s ON s.ProductId=r.ProductId
        WHERE s.CompanyId=@CompanyId AND s.WarehouseId=@WarehouseId AND s.DeletedOn IS NULL AND s.Quantity<r.Quantity)
        THROW 51000, 'Insufficient warehouse stock for the requested seed.', 1;

    INSERT INTO dbo.StockIssues
        (Id,CompanyId,IssueNo,IssueDate,WarehouseId,DepartmentId,ReceivedBy,Note,Status,PostedOn,PostedBy,CreatedOn,CreatedBy,UpdatedOn,UpdatedBy)
    SELECT IssueId,@CompanyId,N'SI-2026-'+UPPER(LEFT(CONVERT(nvarchar(36),IssueId),8)),IssueDate,@WarehouseId,
        DepartmentId,CASE DepartmentId WHEN 2 THEN N'Food Admin' ELSE N'Electronics Admin' END,NULL,
        1,@Now,@UserId,@Now,@UserName,@Now,@UserName FROM @Rows;

    INSERT INTO dbo.StockIssueItems (CompanyId,StockIssueId,ProductId,Quantity,CreatedOn,CreatedBy)
    SELECT @CompanyId,IssueId,ProductId,Quantity,@Now,@UserName FROM @Rows;

    INSERT INTO dbo.StockTransactions
        (CompanyId,ProductId,UserId,WarehouseId,WarehouseLocationId,Quantity,TransactionType,TransactionDate,
         StockIssueId,StockIssueItemId,Note,CreatedOn,CreatedBy)
    SELECT @CompanyId,r.ProductId,@UserId,@WarehouseId,0,r.Quantity,'OUT',r.IssueDate,r.IssueId,i.Id,
        N'Stock Issue: SI-2026-'+UPPER(LEFT(CONVERT(nvarchar(36),r.IssueId),8)),@Now,@UserName
    FROM @Rows r JOIN dbo.StockIssueItems i ON i.StockIssueId=r.IssueId AND i.CompanyId=@CompanyId;

    UPDATE s SET Quantity=s.Quantity-r.Quantity,UpdatedOn=@Now,UpdatedBy=@UserName
    FROM dbo.WarehouseStocks s JOIN @Required r ON r.ProductId=s.ProductId
    WHERE s.CompanyId=@CompanyId AND s.WarehouseId=@WarehouseId AND s.DeletedOn IS NULL;

    UPDATE p SET CurrentStock=(SELECT COALESCE(SUM(s.Quantity),0) FROM dbo.WarehouseStocks s
        WHERE s.CompanyId=@CompanyId AND s.ProductId=p.Id AND s.DeletedOn IS NULL),
        UpdatedOn=@Now,UpdatedBy=@UserName
    FROM dbo.Products p WHERE p.CompanyId=@CompanyId AND p.Id IN (@Rice,@TV);

    IF (SELECT COUNT(*) FROM dbo.StockTransactions WHERE CompanyId=@CompanyId AND StockIssueId IN (SELECT IssueId FROM @Rows)) <> 10
        THROW 51000, 'Ledger verification failed; rolling back the seed.', 1;
    COMMIT;
    SELECT N'Inserted and posted successfully.' AS Result,10 AS SeedIssueCount,65 AS RiceIssued,12 AS TVIssued;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

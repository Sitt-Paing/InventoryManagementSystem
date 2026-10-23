-- Correct metadata on the ten explicitly requested local visualization records.
-- Original seed IDs remain the provenance and idempotency keys; quantities are untouched.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF DB_NAME() <> N'InventoryManagement' THROW 51000, 'Unexpected database.', 1;
DECLARE @CompanyId int=1, @Now datetime=GETDATE();
DECLARE @UserName nvarchar(256)=N'devadmin';
DECLARE @UserId nvarchar(450)=(SELECT Id FROM dbo.AspNetUsers WHERE UserName=@UserName);
IF @UserId IS NULL THROW 51000, 'Expected local administrator was not found.', 1;
DECLARE @Ids TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @Ids VALUES
('183bc14c-fdc4-4ef9-9aa6-edfdbb354c34'),('d98f1ee0-feac-49d5-922f-2f72c0d4f999'),
('0d4291e9-0e72-4981-a694-da454f053081'),('bc4f1426-3ba2-4b84-a4c6-8a42aeab2da2'),
('3b7569c6-2dea-4c85-9457-f51440e3c8ed'),('7df8929f-b571-44e9-b38f-a483e706193a'),
('f69f1e54-4a3f-4ca4-8eb8-0ea2127cf011'),('7211132f-fae5-47f0-b28a-44b15bf60b8e'),
('6acc15a4-b660-43ff-9461-ce899d961294'),('00544e3c-ad11-43c4-9a62-b81437c44734');

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult int;
    EXEC @LockResult=sys.sp_getapplock @Resource=N'DashboardVisualizationSeed20261023',
        @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
    IF @LockResult<0 THROW 51000, 'Unable to lock the seed batch.', 1;
    IF (SELECT COUNT(*) FROM dbo.StockIssues WITH(UPDLOCK,HOLDLOCK)
        WHERE CompanyId=@CompanyId AND Id IN(SELECT Id FROM @Ids) AND DeletedOn IS NULL)<>10
        THROW 51000, 'Expected ten issue records were not found.', 1;
    IF EXISTS(SELECT 1 FROM @Ids r JOIN dbo.StockIssues other
        ON other.IssueNo=N'SI-2026-'+UPPER(LEFT(CONVERT(nvarchar(36),r.Id),8)) AND other.Id<>r.Id)
        THROW 51000, 'A replacement issue number already exists.', 1;

    DECLARE @StockBefore TABLE (Id bigint PRIMARY KEY,Quantity decimal(18,4));
    INSERT INTO @StockBefore SELECT Id,Quantity FROM dbo.WarehouseStocks WITH(UPDLOCK,HOLDLOCK)
    WHERE CompanyId=@CompanyId AND ProductId IN(
        SELECT i.ProductId FROM dbo.StockIssueItems i WHERE i.CompanyId=@CompanyId AND i.StockIssueId IN(SELECT Id FROM @Ids));
    DECLARE @ProductBefore TABLE (Id nvarchar(36) PRIMARY KEY,Quantity decimal(18,4));
    INSERT INTO @ProductBefore SELECT Id,CurrentStock FROM dbo.Products WITH(UPDLOCK,HOLDLOCK)
    WHERE CompanyId=@CompanyId AND Id IN(
        SELECT i.ProductId FROM dbo.StockIssueItems i WHERE i.CompanyId=@CompanyId AND i.StockIssueId IN(SELECT Id FROM @Ids));
    DECLARE @ItemQuantity decimal(18,4)=(SELECT SUM(Quantity) FROM dbo.StockIssueItems
        WHERE CompanyId=@CompanyId AND StockIssueId IN(SELECT Id FROM @Ids));
    DECLARE @LedgerQuantity decimal(18,4)=(SELECT SUM(Quantity) FROM dbo.StockTransactions
        WHERE CompanyId=@CompanyId AND StockIssueId IN(SELECT Id FROM @Ids));

    UPDATE s SET IssueNo=N'SI-2026-'+UPPER(LEFT(CONVERT(nvarchar(36),s.Id),8)),
        ReceivedBy=CASE s.DepartmentId WHEN 2 THEN N'Food Admin' ELSE N'Electronics Admin' END,
        Note=NULL,CreatedBy=@UserName,UpdatedBy=@UserName,UpdatedOn=@Now,PostedBy=@UserId
    FROM dbo.StockIssues s JOIN @Ids r ON r.Id=s.Id WHERE s.CompanyId=@CompanyId;

    UPDATE i SET CreatedBy=@UserName,UpdatedBy=@UserName,UpdatedOn=@Now
    FROM dbo.StockIssueItems i JOIN @Ids r ON r.Id=i.StockIssueId WHERE i.CompanyId=@CompanyId;

    UPDATE t SET Note=N'Stock Issue: '+s.IssueNo,CreatedBy=@UserName,UpdatedBy=@UserName,UpdatedOn=@Now
    FROM dbo.StockTransactions t JOIN dbo.StockIssues s ON s.Id=t.StockIssueId
    JOIN @Ids r ON r.Id=s.Id WHERE t.CompanyId=@CompanyId AND s.CompanyId=@CompanyId;

    UPDATE s SET UpdatedBy=@UserName,UpdatedOn=@Now
    FROM dbo.WarehouseStocks s JOIN @StockBefore b ON b.Id=s.Id
    WHERE s.CompanyId=@CompanyId AND s.UpdatedBy=N'DashboardSeed';
    UPDATE p SET UpdatedBy=@UserName,UpdatedOn=@Now
    FROM dbo.Products p JOIN @ProductBefore b ON b.Id=p.Id
    WHERE p.CompanyId=@CompanyId AND p.UpdatedBy=N'DashboardSeed';

    IF EXISTS(SELECT Id,Quantity FROM @StockBefore EXCEPT SELECT Id,Quantity FROM dbo.WarehouseStocks WHERE CompanyId=@CompanyId)
        THROW 51000, 'Warehouse quantities changed unexpectedly.', 1;
    IF EXISTS(SELECT Id,Quantity FROM @ProductBefore EXCEPT SELECT Id,CurrentStock FROM dbo.Products WHERE CompanyId=@CompanyId)
        THROW 51000, 'Product quantities changed unexpectedly.', 1;
    IF @ItemQuantity<>(SELECT SUM(Quantity) FROM dbo.StockIssueItems WHERE CompanyId=@CompanyId AND StockIssueId IN(SELECT Id FROM @Ids))
        OR @LedgerQuantity<>(SELECT SUM(Quantity) FROM dbo.StockTransactions WHERE CompanyId=@CompanyId AND StockIssueId IN(SELECT Id FROM @Ids))
        THROW 51000, 'Issue or ledger quantities changed unexpectedly.', 1;

    COMMIT;
    SELECT s.IssueNo,s.ReceivedBy,s.CreatedBy,s.UpdatedBy FROM dbo.StockIssues s JOIN @Ids r ON r.Id=s.Id
    WHERE s.CompanyId=@CompanyId ORDER BY s.IssueDate;
    SELECT N'Updated metadata only; stock, issue and ledger quantities are unchanged.' AS Result;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;

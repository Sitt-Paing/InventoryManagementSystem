-- Read-only reconciliation of the explicitly seeded demo batch.
SET NOCOUNT ON;
IF EXISTS(SELECT 1 FROM dbo.Products WHERE SKU='D26-001' AND CompanyId=1 AND CreatedBy='devadmin')
 THROW 51000,'Audit metadata has been normalized. Use verify-normalized-demo.sql with the saved row manifest.',1;
DECLARE @Tag nvarchar(50)=N'DemoRealWorld20261023';
DECLARE @Checks TABLE(CheckName nvarchar(100),Failures int);
INSERT @Checks
SELECT N'Product total equals warehouse balances',COUNT(*) FROM dbo.Products p
OUTER APPLY(SELECT SUM(s.Quantity) Qty FROM dbo.WarehouseStocks s WHERE s.ProductId=p.Id AND s.CompanyId=p.CompanyId AND s.DeletedOn IS NULL)x
WHERE p.CreatedBy=@Tag AND p.CurrentStock<>COALESCE(x.Qty,0);
INSERT @Checks
SELECT N'Warehouse balances equal stock ledger',COUNT(*) FROM dbo.WarehouseStocks s
OUTER APPLY(SELECT SUM(CASE t.TransactionType WHEN 'IN' THEN t.Quantity ELSE -t.Quantity END) Qty FROM dbo.StockTransactions t
 WHERE t.ProductId=s.ProductId AND t.WarehouseId=s.WarehouseId AND t.CompanyId=s.CompanyId AND t.DeletedOn IS NULL)x
WHERE s.CreatedBy=@Tag AND s.Quantity<>COALESCE(x.Qty,0);
INSERT @Checks
SELECT N'PO totals equal line amounts',COUNT(*) FROM dbo.PurchaseOrders p
OUTER APPLY(SELECT SUM(Quantity*UnitPrice) Amount FROM dbo.PurchaseOrderItems i WHERE i.PurchaseOrderId=p.Id AND i.DeletedOn IS NULL)x
WHERE p.CreatedBy=@Tag AND p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks
SELECT N'GRN totals equal line amounts',COUNT(*) FROM dbo.GoodReceipts p
OUTER APPLY(SELECT SUM(LineAmount) Amount FROM dbo.GoodReceiptItems i WHERE i.GoodReceiptId=p.Id AND i.DeletedOn IS NULL)x
WHERE p.CreatedBy=@Tag AND p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks
SELECT N'Return totals equal line amounts',COUNT(*) FROM dbo.PurchaseReturns p
OUTER APPLY(SELECT SUM(ReturnQuantity*UnitPrice) Amount FROM dbo.PurchaseReturnItems i WHERE i.PurchaseReturnId=p.Id AND i.DeletedOn IS NULL)x
WHERE p.CreatedBy=@Tag AND p.TotalAmount<>COALESCE(x.Amount,0);
INSERT @Checks
SELECT N'Each GRN item has one matching IN entry',COUNT(*) FROM dbo.GoodReceiptItems i
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM dbo.StockTransactions t WHERE t.GoodReceiptItemId=i.Id AND t.GoodReceiptId=i.GoodReceiptId
 AND t.CompanyId=i.CompanyId AND t.ProductId=i.ProductId AND t.TransactionType='IN' AND t.DeletedOn IS NULL)x
WHERE i.CreatedBy=@Tag AND (x.C<>1 OR x.Qty<>i.ReceivedBaseQuantity);
INSERT @Checks
SELECT N'Posted issues have matching OUT; drafts have none',COUNT(*) FROM dbo.StockIssueItems i JOIN dbo.StockIssues s ON s.Id=i.StockIssueId
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM dbo.StockTransactions t WHERE t.StockIssueItemId=i.Id AND t.StockIssueId=s.Id
 AND t.CompanyId=i.CompanyId AND t.ProductId=i.ProductId AND t.WarehouseId=s.WarehouseId AND t.TransactionType='OUT' AND t.DeletedOn IS NULL)x
WHERE i.CreatedBy=@Tag AND ((s.Status=1 AND (x.C<>1 OR x.Qty<>i.Quantity)) OR (s.Status=0 AND x.C<>0));
INSERT @Checks
SELECT N'Posted returns have matching OUT; drafts have none',COUNT(*) FROM dbo.PurchaseReturnItems i JOIN dbo.PurchaseReturns s ON s.Id=i.PurchaseReturnId
JOIN dbo.GoodReceiptItems gi ON gi.Id=i.GoodReceiptItemId JOIN dbo.GoodReceipts gr ON gr.Id=gi.GoodReceiptId
OUTER APPLY(SELECT COUNT(*) C,SUM(Quantity) Qty FROM dbo.StockTransactions t WHERE t.PurchaseReturnItemId=i.Id AND t.PurchaseReturnId=s.Id
 AND t.CompanyId=i.CompanyId AND t.ProductId=gi.ProductId AND t.WarehouseId=gr.WarehouseId AND t.TransactionType='OUT' AND t.DeletedOn IS NULL)x
WHERE i.CreatedBy=@Tag AND ((s.Status=1 AND (x.C<>1 OR x.Qty<>i.ReturnQuantity)) OR (s.Status=0 AND x.C<>0));
INSERT @Checks
SELECT N'No product or warehouse balance is negative',COUNT(*) FROM dbo.WarehouseStocks WHERE CreatedBy=@Tag AND Quantity<0;
INSERT @Checks
SELECT N'All demo data belongs to TestCom',COUNT(*) FROM (
 SELECT CompanyId FROM dbo.Products WHERE CreatedBy=@Tag UNION ALL SELECT CompanyId FROM dbo.PurchaseOrders WHERE CreatedBy=@Tag
 UNION ALL SELECT CompanyId FROM dbo.GoodReceipts WHERE CreatedBy=@Tag UNION ALL SELECT CompanyId FROM dbo.PurchaseReturns WHERE CreatedBy=@Tag
 UNION ALL SELECT CompanyId FROM dbo.StockIssues WHERE CreatedBy=@Tag UNION ALL SELECT CompanyId FROM dbo.StockTransactions WHERE CreatedBy=@Tag
)x WHERE CompanyId<>1 OR CompanyId IS NULL;
SELECT CheckName,Failures FROM @Checks;
IF EXISTS(SELECT 1 FROM @Checks WHERE Failures<>0) THROW 51000,'Demo reconciliation failed.',1;
SELECT ApprovalStatus,Status,COUNT(*) Records FROM dbo.PurchaseOrders WHERE CreatedBy=@Tag GROUP BY ApprovalStatus,Status ORDER BY ApprovalStatus,Status;
SELECT MIN(TransactionDate) FirstMovement,MAX(TransactionDate) LastMovement FROM dbo.StockTransactions WHERE CreatedBy=@Tag;
SELECT Name,CurrentStock,ReorderLevel FROM dbo.Products WHERE CreatedBy=@Tag AND CurrentStock<=ReorderLevel ORDER BY Name;

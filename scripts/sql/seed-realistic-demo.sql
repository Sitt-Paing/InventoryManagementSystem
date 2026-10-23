-- Explicit local demo seed. No startup hook, emails, account changes, or existing-data edits.
-- Fictional Myanmar wholesale/office-supply scenario; amounts are illustrative MMK.
-- Run with: ./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/seed-realistic-demo.sql
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF EXISTS(SELECT 1 FROM dbo.Products WHERE CompanyId=1 AND SKU='D26-001' AND CreatedBy='devadmin')
BEGIN
 SELECT N'Normalized demo batch already exists; nothing added.' Result;
 RETURN;
END;
DECLARE @Company int = 1;
DECLARE @Tag nvarchar(50) = N'DemoRealWorld20261023';
DECLARE @Today datetime = '20261023';
DECLARE @Start datetime = DATEADD(day,-90,@Today);
DECLARE @UserId nvarchar(450) = (SELECT Id FROM dbo.AspNetUsers WHERE UserName=N'devadmin');
IF DB_NAME()<>N'InventoryManagement' THROW 51000,'Unexpected database.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Company WHERE Id=@Company AND CompanyName=N'TestCom' AND IsActive=1 AND DeletedOn IS NULL)
    THROW 51000,'Expected active TestCom company was not found.',1;
IF @UserId IS NULL THROW 51000,'Local administrator was not found.',1;

BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource=N'RealisticDemo20261023',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @Lock<0 THROW 51000,'Could not lock the demo seed.',1;
IF EXISTS(SELECT 1 FROM dbo.Products WHERE CreatedBy=@Tag)
BEGIN
    IF (SELECT COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag AND CompanyId=@Company)<>30
       OR (SELECT COUNT(*) FROM dbo.PurchaseOrders WHERE CreatedBy=@Tag AND CompanyId=@Company)<>42
       OR (SELECT COUNT(*) FROM dbo.StockIssues WHERE CreatedBy=@Tag AND CompanyId=@Company)<>66
        THROW 51000,'Incomplete demo batch; inspect before retrying.',1;
    COMMIT;
    SELECT N'Demo batch already exists; no data changed.' AS Result;
    RETURN;
END;
IF EXISTS(SELECT 1 FROM dbo.Suppliers WHERE SupplierCode LIKE N'D26-S%')
   OR EXISTS(SELECT 1 FROM dbo.Products WHERE SKU LIKE 'D26-%')
   OR EXISTS(SELECT 1 FROM dbo.Warehouses WHERE WarehouseCode LIKE N'D26-W%')
    THROW 51000,'Demo codes are already in use.',1;

DECLARE @Groups TABLE(N int PRIMARY KEY,CategoryName nvarchar(100),SupplierName nvarchar(200),Contact nvarchar(100),Address nvarchar(200));
INSERT @Groups VALUES
(1,N'Rice, Pulses & Dry Goods',N'Ayeyar Harvest Distribution (Demo)',N'U Min Htet',N'Bayint Naung Wholesale Market, Yangon'),
(2,N'Cooking Oils & Pantry',N'Golden Pantry Trading (Demo)',N'Daw Thandar Win',N'Shwe Pyi Thar Industrial Zone, Yangon'),
(3,N'Beverages',N'Fresh Valley Beverages (Demo)',N'Ko Aung Kyaw',N'Mingaladon Industrial Park, Yangon'),
(4,N'Household & Cleaning',N'Bright Home Supplies (Demo)',N'Ma Khin Yadanar',N'Hlaing Thar Yar Industrial Zone, Yangon'),
(5,N'Office & Stationery',N'Yangon Office Essentials (Demo)',N'Ko Nay Lin',N'Pabedan Township, Yangon'),
(6,N'Electrical & Small Appliances',N'Mandalay Electrical Trading (Demo)',N'U Zaw Moe',N'Chan Aye Thar Zan Township, Mandalay');
INSERT dbo.Categories(CompanyId,Name,Description,IsActive,CreatedOn,CreatedBy)
SELECT @Company,CategoryName,N'Demo product category',1,@Start,@Tag FROM @Groups;
INSERT dbo.Suppliers(CompanyId,SupplierCode,CompanyName,ContactPerson,Email,Address,PaymentTerms,CreditLimit,Status,CreatedOn,CreatedBy)
SELECT @Company,CONCAT('D26-S0',N),SupplierName,Contact,CONCAT('supplier',N,'@inventory-demo.example'),Address,
    CASE WHEN N%2=0 THEN 'Net 30' ELSE 'Net 15' END,3000000+N*500000,1,@Start,@Tag FROM @Groups;
DECLARE @Categories TABLE(N int PRIMARY KEY,Id bigint);
INSERT @Categories SELECT g.N,c.Id FROM @Groups g JOIN dbo.Categories c ON c.Name=g.CategoryName AND c.CreatedBy=@Tag AND c.CompanyId=@Company;
DECLARE @Suppliers TABLE(N int PRIMARY KEY,Id int);
INSERT @Suppliers SELECT g.N,s.Id FROM @Groups g JOIN dbo.Suppliers s ON s.SupplierCode=CONCAT('D26-S0',g.N) AND s.CompanyId=@Company;

INSERT dbo.UOM_Category(CompanyId,Name,Description,IsActive,CreatedOn,CreatedBy)
VALUES(@Company,N'Demo packaged goods',N'Individual packs and cartons',1,@Start,@Tag);
DECLARE @UomCategory bigint=SCOPE_IDENTITY();
INSERT dbo.UnitOfMeasure(CompanyId,CategoryId,Code,Name,Symbol,DecimalPlaces,IsActive,CreatedOn,CreatedBy)
VALUES(@Company,@UomCategory,'D26-EA',N'Each (pack, bottle or unit)','ea',0,1,@Start,@Tag);
DECLARE @Each bigint=SCOPE_IDENTITY();
INSERT dbo.UnitOfMeasure(CompanyId,CategoryId,Code,Name,Symbol,DecimalPlaces,IsActive,CreatedOn,CreatedBy)
VALUES(@Company,@UomCategory,'D26-CTN',N'Carton','ctn',0,1,@Start,@Tag);
DECLARE @Carton bigint=SCOPE_IDENTITY();

DECLARE @W TABLE(N int PRIMARY KEY,Id int);
INSERT dbo.Warehouses(CompanyId,WarehouseCode,Name,Address,ContactPerson,Email,Capacity,Status,CreatedOn,CreatedBy)
VALUES
(@Company,'D26-W01',N'Yangon Main Distribution Centre',N'Hlaing Thar Yar, Yangon',N'U Myo Aung','yangon@inventory-demo.example',20000,1,@Start,@Tag),
(@Company,'D26-W02',N'Mandalay Regional Warehouse',N'Pyigyitagon, Mandalay',N'Daw Hnin Ei','mandalay@inventory-demo.example',12000,1,@Start,@Tag),
(@Company,'D26-W03',N'Nay Pyi Taw Branch Store',N'Zabuthiri, Nay Pyi Taw',N'Ko Thet Naing','npt@inventory-demo.example',6000,1,@Start,@Tag);
INSERT @W SELECT CONVERT(int,RIGHT(WarehouseCode,2)),Id FROM dbo.Warehouses WHERE CreatedBy=@Tag AND CompanyId=@Company;
INSERT dbo.Warehouse_Locations(CompanyId,WarehouseId,LocationCode,Zone,Rack,Bin,Capacity,Status,CreatedOn,CreatedBy)
SELECT @Company,w.Id,CONCAT('D26-',w.N,'-',v.N),CASE v.N WHEN 1 THEN 'RECEIVING' ELSE 'PICKING' END,
    CONCAT('R0',v.N),'B01',2000,1,@Start,@Tag FROM @W w CROSS JOIN(VALUES(1),(2))v(N);
INSERT dbo.Department(CompanyId,DeptName,IsActive,CreatedOn,CreatedBy)
VALUES(@Company,N'Retail Operations (Demo)',1,@Start,@Tag),(@Company,N'Administration (Demo)',1,@Start,@Tag),
(@Company,N'Housekeeping (Demo)',1,@Start,@Tag),(@Company,N'Staff Canteen (Demo)',1,@Start,@Tag);
DECLARE @Departments TABLE(N int PRIMARY KEY,Id int);
INSERT @Departments SELECT ROW_NUMBER() OVER(ORDER BY Id),Id FROM dbo.Department WHERE CreatedBy=@Tag AND CompanyId=@Company;

DECLARE @P TABLE(N int PRIMARY KEY,Id nvarchar(36),G int,Name nvarchar(250),Brand nvarchar(100),Cost decimal(18,2),Pack int,ReceiptQty int);
INSERT @P(N,G,Name,Brand,Cost,Pack) VALUES
(1,1,N'Paw San Rice 5 kg',N'Ayeyar Harvest',18500,4),(2,1,N'Emata Rice 5 kg',N'Ayeyar Harvest',13500,4),
(3,1,N'Red Lentils 1 kg',N'Harvest Select',6800,12),(4,1,N'Chickpeas 1 kg',N'Harvest Select',7200,12),(5,1,N'Rice Vermicelli 400 g',N'Morning Bowl',2400,24),
(6,2,N'Peanut Oil 1 L',N'Golden Pantry',14500,12),(7,2,N'Sunflower Oil 1 L',N'Golden Pantry',10800,12),
(8,2,N'Iodized Salt 500 g',N'Pantry Select',900,24),(9,2,N'White Sugar 1 kg',N'Pantry Select',3900,12),(10,2,N'Fish Sauce 700 ml',N'Golden Kitchen',3200,12),
(11,3,N'Drinking Water 1 L',N'Fresh Valley',650,12),(12,3,N'Green Tea 500 ml',N'Fresh Valley',1400,24),
(13,3,N'Instant Coffee 20 sachets',N'Highland Cup',6800,12),(14,3,N'Black Tea 250 g',N'Shan Hills',4200,12),(15,3,N'Orange Juice 1 L',N'Fresh Valley',4900,12),
(16,4,N'Dishwashing Liquid 750 ml',N'Bright Home',3800,12),(17,4,N'Laundry Detergent 1 kg',N'Bright Home',6200,12),
(18,4,N'Floor Cleaner 1 L',N'Bright Home',4500,12),(19,4,N'Tissue Box 200 sheets',N'Soft Cloud',2200,24),(20,4,N'Garbage Bags 20 pcs',N'Clean Day',2800,24),
(21,5,N'A4 Copy Paper 80 gsm 500 sheets',N'Office Essentials',12500,5),(22,5,N'Blue Ballpoint Pens 12 pcs',N'Office Essentials',3600,12),
(23,5,N'A4 Lever Arch File',N'Office Essentials',4800,12),(24,5,N'Packing Tape 48 mm x 100 yd',N'Pack Right',2100,36),(25,5,N'Staples No.10 1000 pcs',N'Office Essentials',850,24),
(26,6,N'LED Bulb 12 W',N'Bright Volt',5200,20),(27,6,N'Extension Socket 4 outlets',N'Bright Volt',18500,10),
(28,6,N'Electric Kettle 1.8 L',N'Home Ease',42000,6),(29,6,N'Desk Fan 12 inch',N'Home Ease',68000,4),(30,6,N'AA Batteries 4 pack',N'Power Cell',4200,24);
UPDATE @P SET Id=CONVERT(nvarchar(36),NEWID()),ReceiptQty=CASE WHEN N%5=0 THEN 6 ELSE 24+(N%4)*12 END;
INSERT dbo.Products(Id,CompanyId,Name,SKU,CategoryId,Brand,Barcode,CostPrice,SellingPrice,CurrentStock,ReorderLevel,ReorderQuantity,Tax,BaseUomId,PurchaseUomId,SaleUomId,Status,Description,CreatedOn,CreatedBy)
SELECT p.Id,@Company,p.Name,CONCAT('D26-',RIGHT('000'+CONVERT(varchar,p.N),3)),c.Id,p.Brand,
    CONCAT('DEMO26',RIGHT('000000'+CONVERT(varchar,p.N),6)),p.Cost,ROUND(p.Cost*1.2,0),0,
    CASE WHEN p.N%5=0 THEN 30 ELSE 20 END,p.Pack*4,0,@Each,@Carton,@Each,1,
    N'Fictional demo catalogue. Prices are illustrative MMK per individual pack/unit.',@Start,@Tag
FROM @P p JOIN @Categories c ON c.N=p.G;
INSERT dbo.ProductUomConversion(CompanyId,ProductId,FromUomId,ToUomId,ConversionFactor,IsDefaultPurchase,IsDefaultSale,IsActive,CreatedOn,CreatedBy)
SELECT @Company,Id,@Carton,@Each,Pack,1,0,1,@Start,@Tag FROM @P;

-- Seven orders per supplier: four fulfilled, one partial, one approved/open,
-- and a draft/submitted/rejected/cancelled example. No future receipts.
DECLARE @Orders TABLE(N int PRIMARY KEY,Id uniqueidentifier,G int,Cycle int,W int,D datetime,Approval int,Status int);
INSERT @Orders
SELECT (g.N-1)*7+v.N,NEWID(),g.N,v.N,CASE v.N WHEN 1 THEN 1 WHEN 2 THEN 2 WHEN 3 THEN 3 WHEN 4 THEN 1 ELSE 2 END,
 DATEADD(day,CASE WHEN v.N<=4 THEN -85+(v.N-1)*19+g.N WHEN v.N=5 THEN -18+g.N WHEN v.N=6 THEN -10+g.N ELSE -2 END,@Today),
 CASE WHEN v.N<7 THEN 2 WHEN g.N IN(1,2) THEN 0 WHEN g.N IN(3,4) THEN 1 WHEN g.N=5 THEN 3 ELSE 0 END,
 CASE WHEN v.N<=4 THEN 2 WHEN v.N=5 THEN 1 WHEN v.N=7 AND g.N=6 THEN 3 ELSE 0 END
FROM @Groups g CROSS JOIN(VALUES(1),(2),(3),(4),(5),(6),(7))v(N);
INSERT dbo.PurchaseOrders(Id,CompanyId,PurchaseOrderNo,SupplierId,WarehouseId,OrderDate,ExpectedDate,Status,TotalAmount,ApprovalStatus,
 ApprovedBy,ApprovedOn,RejectedBy,RejectedOn,RejectionReason,CancellationReason,CreatedOn,CreatedBy)
SELECT o.Id,@Company,CONCAT('DEMO-PO-2026-',RIGHT('0000'+CONVERT(varchar,o.N),4)),s.Id,w.Id,o.D,DATEADD(day,CASE WHEN o.Cycle=5 THEN 30 ELSE 5 END,o.D),
 o.Status,0,o.Approval,CASE WHEN o.Approval=2 THEN @UserId END,CASE WHEN o.Approval=2 THEN DATEADD(hour,2,o.D) END,
 CASE WHEN o.Approval=3 THEN @UserId END,CASE WHEN o.Approval=3 THEN DATEADD(hour,4,o.D) END,
 CASE WHEN o.Approval=3 THEN N'Please revise quantities to match the approved monthly budget.' END,
 CASE WHEN o.Status=3 THEN N'Branch replenishment request withdrawn before approval.' END,o.D,@Tag
FROM @Orders o JOIN @Suppliers s ON s.N=o.G JOIN @W w ON w.N=o.W;
INSERT dbo.PurchaseOrderItems(CompanyId,PurchaseOrderId,ProductId,Quantity,UnitPrice,UomId,ReceivedQuantity,CreatedOn,CreatedBy)
SELECT @Company,o.Id,p.Id,p.ReceiptQty,p.Cost,@Each,CASE WHEN o.Cycle<=4 THEN p.ReceiptQty WHEN o.Cycle=5 THEN p.ReceiptQty/2 ELSE 0 END,o.D,@Tag
FROM @Orders o JOIN @P p ON p.G=o.G;
UPDATE po SET TotalAmount=x.Amount FROM dbo.PurchaseOrders po JOIN
 (SELECT PurchaseOrderId,SUM(Quantity*UnitPrice) Amount FROM dbo.PurchaseOrderItems WHERE CreatedBy=@Tag GROUP BY PurchaseOrderId)x ON x.PurchaseOrderId=po.Id;
DECLARE @Receipts TABLE(Id uniqueidentifier,OrderId uniqueidentifier,N int,D datetime,W int,G int);
INSERT @Receipts SELECT NEWID(),Id,N,DATEADD(day,3,D),W,G FROM @Orders WHERE Cycle<=5;
INSERT dbo.GoodReceipts(Id,CompanyId,ReceiptNo,WarehouseId,PurchaseOrderId,SupplierId,ReceiptDate,Status,TotalAmount,ReceivedBy,Note,CreatedOn,CreatedBy)
SELECT r.Id,@Company,CONCAT('DEMO-GRN-2026-',RIGHT('0000'+CONVERT(varchar,r.N),4)),w.Id,r.OrderId,s.Id,r.D,1,0,
 N'U Myo Aung',CASE WHEN o.Cycle=5 THEN N'Partial delivery; balance due with the next scheduled truck.' ELSE N'Quantity and packaging checked at receiving bay.' END,r.D,@Tag
FROM @Receipts r JOIN @W w ON w.N=r.W JOIN @Suppliers s ON s.N=r.G JOIN @Orders o ON o.Id=r.OrderId;
INSERT dbo.GoodReceiptItems(CompanyId,GoodReceiptId,PurchaseOrderItemId,ProductId,UomId,ReceivedQuantity,ReceivedBaseQuantity,UnitPrice,LineAmount,CreatedOn,CreatedBy)
SELECT @Company,r.Id,i.Id,i.ProductId,@Each,i.ReceivedQuantity,i.ReceivedQuantity,i.UnitPrice,i.ReceivedQuantity*i.UnitPrice,r.D,@Tag
FROM @Receipts r JOIN dbo.PurchaseOrderItems i ON i.PurchaseOrderId=r.OrderId AND i.CreatedBy=@Tag;
UPDATE gr SET TotalAmount=x.Amount FROM dbo.GoodReceipts gr JOIN
 (SELECT GoodReceiptId,SUM(LineAmount) Amount FROM dbo.GoodReceiptItems WHERE CreatedBy=@Tag GROUP BY GoodReceiptId)x ON x.GoodReceiptId=gr.Id;
INSERT dbo.StockTransactions(CompanyId,ProductId,UserId,WarehouseId,WarehouseLocationId,Quantity,TransactionType,TransactionDate,GoodReceiptId,GoodReceiptItemId,Note,CreatedOn,CreatedBy)
SELECT @Company,i.ProductId,@UserId,w.Id,0,i.ReceivedBaseQuantity,'IN',r.D,r.Id,i.Id,N'Demo goods receipt',r.D,@Tag
FROM @Receipts r JOIN @W w ON w.N=r.W JOIN dbo.GoodReceiptItems i ON i.GoodReceiptId=r.Id AND i.CreatedBy=@Tag;

DECLARE @Returns TABLE(N int,Id uniqueidentifier,ReceiptItem bigint,D datetime,Posted bit);
INSERT @Returns SELECT g.N,NEWID(),i.Id,DATEADD(day,5,r.D),1
FROM @Groups g JOIN @Receipts r ON r.G=g.N JOIN @Orders o ON o.Id=r.OrderId AND o.Cycle=2
JOIN @P p ON p.G=g.N AND p.N=(g.N-1)*5+1
JOIN dbo.GoodReceiptItems i ON i.GoodReceiptId=r.Id AND i.ProductId=p.Id;
INSERT @Returns SELECT g.N+6,NEWID(),i.Id,DATEADD(day,-1,@Today),0
FROM @Groups g JOIN @Receipts r ON r.G=g.N JOIN @Orders o ON o.Id=r.OrderId AND o.Cycle=5
JOIN @P p ON p.G=g.N AND p.N=(g.N-1)*5+2
JOIN dbo.GoodReceiptItems i ON i.GoodReceiptId=r.Id AND i.ProductId=p.Id WHERE g.N<=2;
INSERT dbo.PurchaseReturns(Id,CompanyId,SupplierId,ReturnNo,ReturnDate,Status,TotalAmount,ReturnReason,PostedOn,PostedBy,CreatedOn,CreatedBy)
SELECT r.Id,@Company,gr.SupplierId,CONCAT('DEMO-PR-2026-',RIGHT('0000'+CONVERT(varchar,r.N),4)),r.D,r.Posted,2*i.UnitPrice,
 CASE WHEN r.Posted=1 THEN N'Two units damaged in transit; supplier agreed to replacement.' ELSE N'Packaging damage reported; awaiting inspection before posting.' END,
 CASE WHEN r.Posted=1 THEN r.D END,CASE WHEN r.Posted=1 THEN @UserId END,r.D,@Tag
FROM @Returns r JOIN dbo.GoodReceiptItems i ON i.Id=r.ReceiptItem JOIN dbo.GoodReceipts gr ON gr.Id=i.GoodReceiptId;
INSERT dbo.PurchaseReturnItems(CompanyId,PurchaseReturnId,GoodReceiptItemId,ReturnQuantity,UnitPrice,LineAmount,CreatedOn,CreatedBy)
SELECT @Company,r.Id,i.Id,2,i.UnitPrice,2*i.UnitPrice,r.D,@Tag FROM @Returns r JOIN dbo.GoodReceiptItems i ON i.Id=r.ReceiptItem;
INSERT dbo.StockTransactions(CompanyId,ProductId,UserId,WarehouseId,WarehouseLocationId,Quantity,TransactionType,TransactionDate,PurchaseReturnId,PurchaseReturnItemId,Note,CreatedOn,CreatedBy)
SELECT @Company,gi.ProductId,@UserId,gr.WarehouseId,0,ri.ReturnQuantity,'OUT',r.D,r.Id,ri.Id,N'Demo purchase return: transit damage',r.D,@Tag
FROM @Returns r JOIN dbo.PurchaseReturnItems ri ON ri.PurchaseReturnId=r.Id JOIN dbo.GoodReceiptItems gi ON gi.Id=ri.GoodReceiptItemId
JOIN dbo.GoodReceipts gr ON gr.Id=gi.GoodReceiptId WHERE r.Posted=1;

DECLARE @Issues TABLE(N int,Id uniqueidentifier,ProductId nvarchar(36),W int,Dept int,D datetime,Quantity int,Posted bit);
INSERT @Issues SELECT (p.N-1)*2+v.N,NEWID(),p.Id,v.N,
 CASE WHEN p.G IN(1,2,3) THEN 4 WHEN p.G=4 THEN 3 WHEN p.G=5 THEN 2 ELSE 1 END,
 DATEADD(day,CASE WHEN v.N=1 THEN -20+(p.N%12) ELSE -6+(p.N%7) END,@Today),CASE v.N WHEN 1 THEN 3 ELSE 5 END,1
FROM @P p CROSS JOIN(VALUES(1),(2))v(N);
INSERT @Issues SELECT 60+G,NEWID(),Id,1,1,@Today,2,0 FROM @P WHERE N%5=1;
INSERT dbo.StockIssues(Id,CompanyId,IssueNo,IssueDate,WarehouseId,DepartmentId,ReceivedBy,Note,Status,PostedOn,PostedBy,CreatedOn,CreatedBy)
SELECT i.Id,@Company,CONCAT('DEMO-SI-2026-',RIGHT('0000'+CONVERT(varchar,i.N),4)),i.D,w.Id,d.Id,
 CASE i.Dept WHEN 1 THEN N'Ko Nay Lin' WHEN 2 THEN N'Ma Ei Mon' WHEN 3 THEN N'Ma Su Su' ELSE N'U Hla Win' END,
 CASE WHEN i.Posted=1 THEN N'Weekly department replenishment.' ELSE N'Next replenishment request awaiting warehouse posting.' END,i.Posted,
 CASE WHEN i.Posted=1 THEN i.D END,CASE WHEN i.Posted=1 THEN @UserId END,i.D,@Tag
FROM @Issues i JOIN @W w ON w.N=i.W JOIN @Departments d ON d.N=i.Dept;
INSERT dbo.StockIssueItems(CompanyId,StockIssueId,ProductId,Quantity,CreatedOn,CreatedBy)
SELECT @Company,Id,ProductId,Quantity,D,@Tag FROM @Issues;
INSERT dbo.StockTransactions(CompanyId,ProductId,UserId,WarehouseId,WarehouseLocationId,Quantity,TransactionType,TransactionDate,StockIssueId,StockIssueItemId,Note,CreatedOn,CreatedBy)
SELECT @Company,i.ProductId,@UserId,w.Id,0,i.Quantity,'OUT',i.D,i.Id,ii.Id,N'Demo department replenishment',i.D,@Tag
FROM @Issues i JOIN @W w ON w.N=i.W JOIN dbo.StockIssueItems ii ON ii.StockIssueId=i.Id WHERE i.Posted=1;

-- Only the newly created products are included; existing balances are untouched.
INSERT dbo.WarehouseStocks(CompanyId,ProductId,WarehouseId,Quantity,CreatedOn,CreatedBy)
SELECT @Company,t.ProductId,t.WarehouseId,SUM(CASE t.TransactionType WHEN 'IN' THEN t.Quantity ELSE -t.Quantity END),@Today,@Tag
FROM dbo.StockTransactions t JOIN @P p ON p.Id=t.ProductId WHERE t.CreatedBy=@Tag GROUP BY t.ProductId,t.WarehouseId;
UPDATE p SET CurrentStock=x.Quantity FROM dbo.Products p JOIN
 (SELECT ProductId,SUM(Quantity) Quantity FROM dbo.WarehouseStocks WHERE CreatedBy=@Tag GROUP BY ProductId)x ON x.ProductId=p.Id WHERE p.CreatedBy=@Tag;

-- Fail the whole batch if documents, tenant links, or the running ledger disagree.
IF (SELECT COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag)<>30
 OR (SELECT COUNT(*) FROM dbo.GoodReceiptItems WHERE CreatedBy=@Tag)<>150
 OR (SELECT COUNT(*) FROM dbo.StockTransactions WHERE CreatedBy=@Tag)<>216
 THROW 51000,'Unexpected demo row counts.',1;
IF EXISTS(SELECT 1 FROM dbo.WarehouseStocks WHERE CreatedBy=@Tag AND Quantity<0)
 THROW 51000,'Negative warehouse balance.',1;
IF EXISTS(SELECT 1 FROM (
 SELECT SUM(CASE TransactionType WHEN 'IN' THEN Quantity ELSE -Quantity END) OVER
 (PARTITION BY ProductId,WarehouseId ORDER BY TransactionDate,Id ROWS UNBOUNDED PRECEDING) Balance
 FROM dbo.StockTransactions WHERE CreatedBy=@Tag)x WHERE Balance<0)
 THROW 51000,'Historical stock would become negative.',1;
IF EXISTS(SELECT 1 FROM dbo.PurchaseOrderItems i LEFT JOIN (
 SELECT PurchaseOrderItemId,SUM(ReceivedQuantity) Qty FROM dbo.GoodReceiptItems WHERE CreatedBy=@Tag GROUP BY PurchaseOrderItemId)x ON x.PurchaseOrderItemId=i.Id
 WHERE i.CreatedBy=@Tag AND (i.ReceivedQuantity<>COALESCE(x.Qty,0) OR i.ReceivedQuantity>i.Quantity))
 THROW 51000,'Purchase order receipt quantities disagree.',1;
IF EXISTS(SELECT 1 FROM dbo.GoodReceiptItems i JOIN dbo.PurchaseOrderItems po ON po.Id=i.PurchaseOrderItemId
 JOIN dbo.GoodReceipts gr ON gr.Id=i.GoodReceiptId JOIN dbo.Products p ON p.Id=i.ProductId
 WHERE i.CreatedBy=@Tag AND (i.CompanyId<>@Company OR po.CompanyId<>@Company OR gr.CompanyId<>@Company OR p.CompanyId<>@Company
 OR po.PurchaseOrderId<>gr.PurchaseOrderId OR po.ProductId<>i.ProductId OR i.LineAmount<>i.UnitPrice*i.ReceivedQuantity))
 THROW 51000,'Receipt relationship or amount check failed.',1;
IF EXISTS(SELECT 1 FROM dbo.StockTransactions t LEFT JOIN dbo.StockIssues si ON si.Id=t.StockIssueId
 LEFT JOIN dbo.PurchaseReturns pr ON pr.Id=t.PurchaseReturnId WHERE t.CreatedBy=@Tag AND (si.Status=0 OR pr.Status=0))
 THROW 51000,'Draft documents must not change stock.',1;
COMMIT;
SELECT N'Demo data inserted successfully' AS Result;
SELECT 'Products' Entity,COUNT(*) Records FROM dbo.Products WHERE CreatedBy=@Tag
UNION ALL SELECT 'Suppliers',COUNT(*) FROM dbo.Suppliers WHERE CreatedBy=@Tag
UNION ALL SELECT 'Categories',COUNT(*) FROM dbo.Categories WHERE CreatedBy=@Tag
UNION ALL SELECT 'Warehouses',COUNT(*) FROM dbo.Warehouses WHERE CreatedBy=@Tag
UNION ALL SELECT 'Departments',COUNT(*) FROM dbo.Department WHERE CreatedBy=@Tag
UNION ALL SELECT 'Purchase orders',COUNT(*) FROM dbo.PurchaseOrders WHERE CreatedBy=@Tag
UNION ALL SELECT 'Goods receipts',COUNT(*) FROM dbo.GoodReceipts WHERE CreatedBy=@Tag
UNION ALL SELECT 'Purchase returns',COUNT(*) FROM dbo.PurchaseReturns WHERE CreatedBy=@Tag
UNION ALL SELECT 'Stock issues',COUNT(*) FROM dbo.StockIssues WHERE CreatedBy=@Tag
UNION ALL SELECT 'Stock ledger entries',COUNT(*) FROM dbo.StockTransactions WHERE CreatedBy=@Tag
UNION ALL SELECT 'Low-stock products',COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag AND CurrentStock<=ReorderLevel;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;

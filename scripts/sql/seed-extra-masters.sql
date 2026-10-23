-- Additional master-only examples for TestCom. All businesses/contacts are fictional.
-- Run explicitly with Invoke-LocalInventorySql.ps1; no opening stock or emails.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF EXISTS(SELECT 1 FROM dbo.Products WHERE CompanyId=1 AND SKU='M26-P01' AND CreatedBy='devadmin')
BEGIN
 SELECT N'Normalized master batch already exists; nothing added.' Result;
 RETURN;
END;
DECLARE @Tag nvarchar(50)=N'DemoMasters20261023',@Company int=1,@Now datetime='20261023';
IF DB_NAME()<>N'InventoryManagement' THROW 51000,'Unexpected database.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.Company WHERE Id=@Company AND CompanyName=N'TestCom' AND IsActive=1 AND DeletedOn IS NULL)
 THROW 51000,'Expected TestCom company not found.',1;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Lock int;
EXEC @Lock=sys.sp_getapplock @Resource=@Tag,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
IF @Lock<0 THROW 51000,'Unable to lock master seed.',1;
IF EXISTS(SELECT 1 FROM dbo.UOM_Category WHERE CreatedBy=@Tag)
BEGIN
 IF (SELECT COUNT(*) FROM dbo.UOM_Category WHERE CreatedBy=@Tag)<>4
 OR (SELECT COUNT(*) FROM dbo.UnitOfMeasure WHERE CreatedBy=@Tag)<>14
 OR (SELECT COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag)<>12
 OR (SELECT COUNT(*) FROM dbo.ProductUomConversion WHERE CreatedBy=@Tag)<>12
 OR (SELECT COUNT(*) FROM dbo.Categories WHERE CreatedBy=@Tag)<>4
 OR (SELECT COUNT(*) FROM dbo.Suppliers WHERE CreatedBy=@Tag)<>4
 OR (SELECT COUNT(*) FROM dbo.Warehouses WHERE CreatedBy=@Tag)<>2
 OR (SELECT COUNT(*) FROM dbo.Warehouse_Locations WHERE CreatedBy=@Tag)<>8
 OR (SELECT COUNT(*) FROM dbo.Department WHERE CreatedBy=@Tag)<>5
 THROW 51000,'Incomplete master demo batch; inspect before retrying.',1;
 COMMIT;
 SELECT N'Master batch already exists; no changes.' Result;
 RETURN;
END;
IF EXISTS(SELECT 1 FROM dbo.Products WHERE SKU LIKE 'M26-%')
 OR EXISTS(SELECT 1 FROM dbo.UnitOfMeasure WHERE Code LIKE 'M26-%')
 OR EXISTS(SELECT 1 FROM dbo.Suppliers WHERE SupplierCode LIKE 'M26-%')
 OR EXISTS(SELECT 1 FROM dbo.Warehouses WHERE WarehouseCode LIKE 'M26-%')
 THROW 51000,'Master demo codes already in use.',1;

INSERT dbo.UOM_Category(CompanyId,Name,Description,IsActive,CreatedOn,CreatedBy) VALUES
(@Company,N'Mass / Weight (Demo)',N'Bulk goods measured by weight',1,@Now,@Tag),
(@Company,N'Liquid Volume (Demo)',N'Liquids measured by volume',1,@Now,@Tag),
(@Company,N'Length (Demo)',N'Cables and packaging measured by length',1,@Now,@Tag),
(@Company,N'Count & Packs (Demo)',N'Individual units and supplier packs',1,@Now,@Tag);
DECLARE @Units TABLE(Code nvarchar(20),Name nvarchar(150),Symbol nvarchar(20),Places int,Category nvarchar(100));
INSERT @Units VALUES
('M26-KG',N'Kilogram','kg',3,N'Mass / Weight (Demo)'),('M26-G',N'Gram','g',0,N'Mass / Weight (Demo)'),
('M26-BAG50',N'50 kg Bag','bag',0,N'Mass / Weight (Demo)'),('M26-T',N'Metric Tonne','t',3,N'Mass / Weight (Demo)'),
('M26-L',N'Litre','L',3,N'Liquid Volume (Demo)'),('M26-ML',N'Millilitre','ml',0,N'Liquid Volume (Demo)'),
('M26-DRUM20',N'20 L Drum','drum',0,N'Liquid Volume (Demo)'),
('M26-M',N'Metre','m',2,N'Length (Demo)'),('M26-CM',N'Centimetre','cm',0,N'Length (Demo)'),
('M26-ROLL100',N'100 m Roll','roll',0,N'Length (Demo)'),
('M26-EA',N'Each','ea',0,N'Count & Packs (Demo)'),('M26-BOX12',N'Box of 12','box',0,N'Count & Packs (Demo)'),
('M26-PAIR',N'Pair','pair',0,N'Count & Packs (Demo)'),('M26-CTN24',N'Carton of 24','ctn',0,N'Count & Packs (Demo)');
INSERT dbo.UnitOfMeasure(CompanyId,CategoryId,Code,Name,Symbol,DecimalPlaces,IsActive,CreatedOn,CreatedBy)
SELECT @Company,c.Id,u.Code,u.Name,u.Symbol,u.Places,1,@Now,@Tag FROM @Units u
JOIN dbo.UOM_Category c ON c.Name=u.Category AND c.CompanyId=@Company AND c.CreatedBy=@Tag;
INSERT dbo.Categories(CompanyId,Name,Description,IsActive,CreatedOn,CreatedBy) VALUES
(@Company,N'Bulk Ingredients (Demo)',N'Loose commodities purchased in bags and sold by weight',1,@Now,@Tag),
(@Company,N'Industrial Liquids (Demo)',N'Bulk cleaning and maintenance liquids',1,@Now,@Tag),
(@Company,N'Cables & Packaging Materials (Demo)',N'Materials purchased by roll and issued by metre',1,@Now,@Tag),
(@Company,N'Workshop & Safety Supplies (Demo)',N'Consumables and personal protective equipment',1,@Now,@Tag);
INSERT dbo.Suppliers(CompanyId,SupplierCode,CompanyName,ContactPerson,Email,Address,PaymentTerms,CreditLimit,Status,CreatedOn,CreatedBy) VALUES
(@Company,'M26-S01',N'Delta Bulk Ingredients (Demo)',N'U Ye Htut','bulk@master-demo.example',N'Pathein Industrial Zone, Ayeyarwady','Net 15',5000000,1,@Now,@Tag),
(@Company,'M26-S02',N'Thazin Industrial Solutions (Demo)',N'Daw Thazin Aye','liquids@master-demo.example',N'South Dagon Industrial Zone, Yangon','Net 30',3500000,1,@Now,@Tag),
(@Company,'M26-S03',N'Unity Cable & Packaging (Demo)',N'Ko Wai Yan','materials@master-demo.example',N'Pyigyitagon Industrial Zone, Mandalay','Cash on Delivery',0,1,@Now,@Tag),
(@Company,'M26-S04',N'Safe Hands Workshop Supplies (Demo)',N'Ma May Thu','safety@master-demo.example',N'Taunggyi, Shan State','Net 30',1500000,1,@Now,@Tag);
INSERT dbo.Warehouses(CompanyId,WarehouseCode,Name,Address,ContactPerson,Email,Capacity,Status,CreatedOn,CreatedBy) VALUES
(@Company,'M26-W01',N'Bago Bulk Materials Depot (Demo)',N'Bago Industrial Zone, Bago',N'U Kyaw Soe','bago@master-demo.example',15000,1,@Now,@Tag),
(@Company,'M26-W02',N'Taunggyi Service Store (Demo)',N'Aye Thar Yar, Taunggyi',N'Daw Nwe Nwe','taunggyi@master-demo.example',4000,1,@Now,@Tag);
INSERT dbo.Warehouse_Locations(CompanyId,WarehouseId,LocationCode,Zone,Rack,Bin,Barcode,Capacity,Status,CreatedOn,CreatedBy)
SELECT @Company,w.Id,CONCAT(w.WarehouseCode,'-',v.Code),v.Zone,v.Rack,v.Bin,CONCAT('LOC-',w.WarehouseCode,'-',v.Code),v.Capacity,1,@Now,@Tag
FROM dbo.Warehouses w CROSS JOIN(VALUES
 ('RCV','Receiving','R01','B01',1000),('BULK','Bulk Storage','R02','B01',3000),
 ('PICK','Picking','R03','B01',500),('QC','Quality Inspection','R04','B01',250))v(Code,Zone,Rack,Bin,Capacity)
WHERE w.CreatedBy=@Tag AND w.CompanyId=@Company;
INSERT dbo.Department(CompanyId,DeptName,IsActive,CreatedOn,CreatedBy) VALUES
(@Company,N'Procurement (Demo)',1,@Now,@Tag),(@Company,N'Quality Control (Demo)',1,@Now,@Tag),
(@Company,N'Maintenance & Engineering (Demo)',1,@Now,@Tag),(@Company,N'Logistics & Dispatch (Demo)',1,@Now,@Tag),
(@Company,N'Production & Repacking (Demo)',1,@Now,@Tag);

DECLARE @P TABLE(N int PRIMARY KEY,Id nvarchar(36),Name nvarchar(250),Category nvarchar(100),BaseCode nvarchar(20),PurchaseCode nvarchar(20),Factor decimal(18,6),Cost decimal(18,2),Brand nvarchar(100));
INSERT @P(N,Name,Category,BaseCode,PurchaseCode,Factor,Cost,Brand) VALUES
(1,N'Loose Emata Rice',N'Bulk Ingredients (Demo)','M26-KG','M26-BAG50',50,2700,N'Delta Harvest'),
(2,N'Wheat Flour',N'Bulk Ingredients (Demo)','M26-KG','M26-BAG50',50,4200,N'Delta Mill'),
(3,N'Yellow Split Peas',N'Bulk Ingredients (Demo)','M26-KG','M26-BAG50',50,5800,N'Delta Harvest'),
(4,N'Bulk Floor Cleaning Solution',N'Industrial Liquids (Demo)','M26-L','M26-DRUM20',20,3200,N'Thazin Care'),
(5,N'Liquid Hand Soap Refill',N'Industrial Liquids (Demo)','M26-L','M26-DRUM20',20,4600,N'Thazin Care'),
(6,N'Machine Lubricating Oil',N'Industrial Liquids (Demo)','M26-L','M26-DRUM20',20,12500,N'Thazin Industrial'),
(7,N'Electrical Cable 2.5 sq mm',N'Cables & Packaging Materials (Demo)','M26-M','M26-ROLL100',100,2400,N'Unity Cable'),
(8,N'Polypropylene Packing Strap',N'Cables & Packaging Materials (Demo)','M26-M','M26-ROLL100',100,180,N'Unity Pack'),
(9,N'Cotton Binding Rope 6 mm',N'Cables & Packaging Materials (Demo)','M26-M','M26-ROLL100',100,650,N'Unity Pack'),
(10,N'Industrial Safety Goggles',N'Workshop & Safety Supplies (Demo)','M26-EA','M26-BOX12',12,4800,N'Safe Hands'),
(11,N'Dust Mask',N'Workshop & Safety Supplies (Demo)','M26-EA','M26-CTN24',24,1200,N'Safe Hands'),
(12,N'Cotton Work Gloves',N'Workshop & Safety Supplies (Demo)','M26-PAIR','M26-BOX12',12,1800,N'Safe Hands');
UPDATE @P SET Id=CONVERT(nvarchar(36),NEWID());
INSERT dbo.Products(Id,CompanyId,Name,SKU,CategoryId,Brand,Barcode,CostPrice,SellingPrice,CurrentStock,ReorderLevel,ReorderQuantity,Tax,BaseUomId,PurchaseUomId,SaleUomId,Status,Description,CreatedOn,CreatedBy)
SELECT p.Id,@Company,p.Name,CONCAT('M26-P',RIGHT('00'+CONVERT(varchar,p.N),2)),c.Id,p.Brand,CONCAT('MASTER26',RIGHT('0000'+CONVERT(varchar,p.N),4)),p.Cost,ROUND(p.Cost*1.2,0),0,
 p.Factor,p.Factor*2,0,b.Id,u.Id,b.Id,1,
 CONCAT(N'Master demo: 1 ',u.Name,' = ',CONVERT(varchar,p.Factor),' ',b.Name,N'. No opening stock. Illustrative MMK per base unit.'),@Now,@Tag
FROM @P p JOIN dbo.Categories c ON c.Name=p.Category AND c.CompanyId=@Company AND c.CreatedBy=@Tag
JOIN dbo.UnitOfMeasure b ON b.Code=p.BaseCode AND b.CompanyId=@Company AND b.CreatedBy=@Tag
JOIN dbo.UnitOfMeasure u ON u.Code=p.PurchaseCode AND u.CompanyId=@Company AND u.CreatedBy=@Tag;
INSERT dbo.ProductUomConversion(CompanyId,ProductId,FromUomId,ToUomId,ConversionFactor,Barcode,IsDefaultPurchase,IsDefaultSale,IsActive,CreatedOn,CreatedBy)
SELECT @Company,p.Id,u.Id,b.Id,p.Factor,CONCAT('PACK26',RIGHT('0000'+CONVERT(varchar,p.N),4)),1,0,1,@Now,@Tag
FROM @P p JOIN dbo.UnitOfMeasure b ON b.Code=p.BaseCode AND b.CompanyId=@Company AND b.CreatedBy=@Tag
JOIN dbo.UnitOfMeasure u ON u.Code=p.PurchaseCode AND u.CompanyId=@Company AND u.CreatedBy=@Tag;
IF (SELECT COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag)<>12
 OR (SELECT COUNT(*) FROM dbo.ProductUomConversion WHERE CreatedBy=@Tag)<>12
 OR (SELECT COUNT(*) FROM dbo.UnitOfMeasure WHERE CreatedBy=@Tag)<>14
 THROW 51000,'Master row counts disagree.',1;
IF EXISTS(SELECT 1 FROM dbo.ProductUomConversion x JOIN dbo.UnitOfMeasure f ON f.Id=x.FromUomId JOIN dbo.UnitOfMeasure t ON t.Id=x.ToUomId
 JOIN dbo.Products p ON p.Id=x.ProductId WHERE x.CreatedBy=@Tag AND
 (x.CompanyId<>@Company OR f.CompanyId<>@Company OR t.CompanyId<>@Company OR p.CompanyId<>@Company OR f.CategoryId<>t.CategoryId
 OR x.ConversionFactor<=0 OR x.ToUomId<>p.BaseUomId OR x.FromUomId<>p.PurchaseUomId))
 THROW 51000,'UOM conversion relationship failed.',1;
COMMIT;
SELECT N'Additional master data inserted successfully.' Result;
SELECT 'UOM categories' Entity,COUNT(*) Records FROM dbo.UOM_Category WHERE CreatedBy=@Tag
UNION ALL SELECT 'Units of measure',COUNT(*) FROM dbo.UnitOfMeasure WHERE CreatedBy=@Tag
UNION ALL SELECT 'Product categories',COUNT(*) FROM dbo.Categories WHERE CreatedBy=@Tag
UNION ALL SELECT 'Suppliers',COUNT(*) FROM dbo.Suppliers WHERE CreatedBy=@Tag
UNION ALL SELECT 'Warehouses',COUNT(*) FROM dbo.Warehouses WHERE CreatedBy=@Tag
UNION ALL SELECT 'Warehouse locations',COUNT(*) FROM dbo.Warehouse_Locations WHERE CreatedBy=@Tag
UNION ALL SELECT 'Departments',COUNT(*) FROM dbo.Department WHERE CreatedBy=@Tag
UNION ALL SELECT 'Products',COUNT(*) FROM dbo.Products WHERE CreatedBy=@Tag
UNION ALL SELECT 'Product UOM conversions',COUNT(*) FROM dbo.ProductUomConversion WHERE CreatedBy=@Tag;
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK;
THROW;
END CATCH;

# Realistic local demo data

This explicit seed adds fictional Myanmar distribution data to **TestCom (company 1)** in the local `InventoryManagement` database. It preserves existing records and accounts. Nothing runs automatically on API startup, and no email outbox records are created.

The batch contains 30 packaged-goods, beverage, cleaning, stationery, and electrical products; 6 suppliers/categories; 3 warehouses with 6 locations; 4 departments; 2 units of measure with 30 carton conversions; 42 purchase orders; 30 goods receipts; 8 purchase returns; 66 stock issues; and 216 stock movements. Prices are illustrative MMK, not market quotations. Supplier names and `.example` contact addresses are fictional.

Orders include completed, partially received, approved/open, draft, submitted, rejected, and cancelled examples. Six returns and 60 issues are posted; remaining drafts do not affect stock. Six products are below reorder level. History covers late July through October 23, 2026. Catalogue codes use `D26-`. Document numbers and audit fields have subsequently been normalized as described below.

From the repository root in PowerShell:

```powershell
./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/seed-realistic-demo.sql
./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/verify-normalized-demo.sql
```

The runner reads the API's local user-secret connection (or `ConnectionStrings__DefaultConnection`) and rejects nonlocal servers or a different database. The seed executes in one transaction with an application lock. Repeating the completed seed does not add records or change stock. Validation checks quantities, totals, posting rules, company ownership, and stock reconciliation.

Sign in with an existing authorized account, select **TestCom**, and refresh the relevant pages to view the data.

## Additional master examples

`seed-extra-masters.sql` added a separate master batch to TestCom, identified by `M26-` codes and saved row IDs:

- 4 UOM categories (weight, volume, length, count) and 14 units of measure.
- 4 product categories, 4 fictional suppliers, 2 warehouses, 8 warehouse locations, and 5 departments.
- 12 products with 12 purchase-to-base conversions, including 50 kg bags, 20 L drums, 100 m rolls, boxes of 12, and cartons of 24.

The additional products initially had zero stock. `seed-master-purchases.sql` subsequently added 4 completed purchase orders, 4 goods receipts, and 12 receipt lines using the purchase pack conversions. The 12 products now have matching warehouse and ledger balances. Master creation dates precede these orders. No account changes or outbound emails were created.

```powershell
./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/seed-extra-masters.sql
./scripts/sql/Test-ExtraMastersApi.ps1
```

The API smoke check requires the API running at `https://localhost:7152` and the existing administrator password in API user secrets. It uses a separate cookie session to select TestCom. It verifies all added master lists, product details, name search, product/UOM/warehouse filters, purchase/base unit links, and product/location barcode lookups. It also reads the normalized PO/GRN/return/issue lists and document details. It trusts the local development certificate only for these localhost requests. Repeating the seed does not duplicate the batch.

## Default numbers and audit names

All 19 affected master/document/detail/ledger tables use `devadmin` for both CreatedBy and UpdatedBy on the seeded rows. Original batch identifiers and row IDs are kept in `demo-data-manifest.json`, outside user-facing audit columns. Preserve this file: the runner loads it into a temporary table for exact row scoping. The original seed scripts are historical generators and detect the already-normalized batches before inserting anything.

Document formats match the current application:

- PO: `PO-2026-00000001` (database sequence; existing legacy PO numbers are preserved).
- GRN: `GRN-123456` (six-digit millisecond suffix).
- Return: `PR-2026-00000003` onwards, after the two original returns.
- Issue: `SI-2026-XXXXXXXX` (eight uppercase hexadecimal characters).

Stock ledger descriptions reference the corrected document numbers. Existing non-demo records are unchanged. After the additions there are 46 seeded purchase orders, 34 goods receipts, 8 returns, 66 issues, and 228 stock movements for 42 seeded products.

```powershell
./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/normalize-demo-data.sql
./scripts/sql/Invoke-LocalInventorySql.ps1 -Path ./scripts/sql/verify-normalized-demo.sql
./scripts/sql/Test-ExtraMastersApi.ps1
```

Normalization is transactional and preserves quantities and money amounts. Re-running it preserves document numbers. The old `verify-realistic-demo.sql` deliberately directs callers to the manifest-based verifier after normalization, so changed audit names cannot cause a misleading empty verification result.

`clean-demo-labels-barcodes.sql` removes `(Demo)` from seeded master names and fills missing product, packaging and warehouse-location barcodes. All 42 products, 42 packaging conversions and 14 locations now have barcodes. This cleanup preserves audit user `devadmin`, document numbers, prices and stock quantities. Products, packaging labels and stock movements display UOM symbols instead of internal UOM codes.

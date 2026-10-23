# Read the running local API using an isolated login session. Never prints credentials/tokens.
$ErrorActionPreference = 'Stop'
$base = 'https://localhost:7152'
$tag = 'DemoMasters20261023'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'demo-data-manifest.json') -Raw | ConvertFrom-Json
$settings = Get-Content -LiteralPath (Join-Path $env:APPDATA 'Microsoft/UserSecrets/49be33f5-73a8-46e3-913b-9f8a7d5988be/secrets.json') -Raw | ConvertFrom-Json
$userName = $settings.'DefaultAdmin:UserName'
if (!$userName) { $userName = 'devadmin' }
if (!$settings.'DefaultAdmin:Password') { throw 'The configured local administrator password is required.' }
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
function Get-ApiData([string]$path) {
    $response = Invoke-RestMethod -Uri ($base + $path) -WebSession $session -SkipCertificateCheck -Headers @{'X-Company-Id'='1'}
    if (!$response.success) { throw "API query failed: $path" }
    return $response.data
}
function Assert-Count($rows, [int]$expected, [string]$label) {
    if (@($rows).Count -ne $expected) { throw "$label expected $expected rows; got $(@($rows).Count)." }
}
$csrf = Invoke-RestMethod -Uri "$base/api/Auth/csrf-token" -WebSession $session -SkipCertificateCheck
$loginBody = @{userNameOrEmail=$userName; password=$settings.'DefaultAdmin:Password'; rememberMe=$false} | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -WebSession $session -SkipCertificateCheck -ContentType 'application/json' -Headers @{'X-XSRF-TOKEN'=$csrf.data.csrfToken} -Body $loginBody
if (!$login.success) { throw 'Local administrator login failed.' }
$csrf = Invoke-RestMethod -Uri "$base/api/Auth/csrf-token" -WebSession $session -SkipCertificateCheck
$selected = Invoke-RestMethod -Uri "$base/api/Auth/select-company" -Method Post -WebSession $session -SkipCertificateCheck -ContentType 'application/json' -Headers @{'X-XSRF-TOKEN'=$csrf.data.csrfToken} -Body '{"companyId":1}'
if (!$selected.success) { throw 'TestCom selection failed.' }

$expectedLists = @{
    'categories'=4; 'suppliers'=4; 'warehouses'=2; 'warehouse-locations'=8
    'departments'=5; 'uom-categories'=4; 'unit-of-measures'=14
}
$lists = @{}
$tableNames = @{'categories'='Categories';'suppliers'='Suppliers';'warehouses'='Warehouses';'warehouse-locations'='Warehouse_Locations';'departments'='Department';'uom-categories'='UOM_Category';'unit-of-measures'='UnitOfMeasure'}
foreach ($name in $expectedLists.Keys) {
    $ids = @($manifest | Where-Object { $_.TableName -eq $tableNames[$name] -and $_.Batch -eq $tag } | ForEach-Object Id)
    $rows = @(Get-ApiData "/api/master/$name" | Where-Object { [string]$_.id -in $ids })
    Assert-Count $rows $expectedLists[$name] $name
    foreach ($row in $rows) {
        if (($row.name, $row.companyName, $row.deptName, $row.warehouseName, $row.categoryName -join ' ') -match '\(Demo\)') {
            throw "Demo label remains in $name."
        }
    }
    $lists[$name] = $rows
    Write-Output "$name : PASS ($($rows.Count) added records)"
}
$allProducts = Get-ApiData '/api/master/products'
$productIds = @($manifest | Where-Object { $_.TableName -eq 'Products' -and $_.Batch -eq $tag } | ForEach-Object Id)
$products = @($allProducts.items | Where-Object { [string]$_.id -in $productIds })
Assert-Count $products 12 'Product list'
$searched = Get-ApiData '/api/master/products?search=Loose%20Emata&pageNumber=1&pageSize=10'
Assert-Count $searched.items 1 'Product name search'
if ($searched.items[0].sku -ne 'M26-P01') { throw 'Product name search mismatch.' }
foreach ($p in $products) {
    $detail = Get-ApiData "/api/master/products/$($p.id)"
    if ($detail.createdBy -ne 'devadmin' -or $detail.updatedBy -ne 'devadmin' -or $detail.currentStock -le 0) { throw 'Product detail, audit user or received stock mismatch.' }
    $conversions = @(Get-ApiData "/api/master/product-uom-conversions/product/$($p.id)")
    Assert-Count $conversions 1 'Product conversion'
    $c = $conversions[0]
    if ($c.toUomId -ne $detail.baseUomId -or $c.fromUomId -ne $detail.purchaseUomId -or $c.conversionFactor -le 0) {
        throw 'Purchase/base conversion mismatch.'
    }
    $lookup = Get-ApiData "/api/master/products/barcode-lookup?code=$($p.barcode)"
    # The lookup envelope contains a typed Product property.
    if ($lookup.product.productId -ne $p.id) { throw 'Product barcode lookup mismatch.' }
}
Write-Output 'Products : PASS (12 searchable products, details, conversions and barcodes)'
foreach ($category in $lists['uom-categories']) {
    $units = @(Get-ApiData "/api/master/unit-of-measures?categoryId=$($category.id)")
    if (!$units.Count -or @($units | Where-Object categoryId -ne $category.id).Count) { throw 'UOM category filter failed.' }
}
Write-Output 'UOM category filters : PASS (4 categories)'
foreach ($category in $lists['categories']) {
    $filtered = Get-ApiData "/api/master/products?categoryId=$($category.id)"
    Assert-Count $filtered.items 3 'Product category filter'
}
Write-Output 'Product category filters : PASS (4 categories, 3 products each)'
foreach ($warehouse in $lists['warehouses']) {
    $locations = @(Get-ApiData "/api/master/warehouse-locations?warehouseId=$($warehouse.id)")
    Assert-Count $locations 4 'Warehouse locations filter'
    if (@($locations | Where-Object warehouseId -ne $warehouse.id).Count) { throw 'Warehouse location scope failed.' }
    foreach ($location in $locations) {
        $lookup = Get-ApiData "/api/master/products/barcode-lookup?code=$($location.barcode)"
        if ($lookup.location.warehouseLocationId -ne $location.id) { throw 'Location barcode lookup failed.' }
    }
}
Write-Output 'Warehouse filters and location barcodes : PASS (2 warehouses, 4 locations each)'
$locationIds = @($manifest | Where-Object TableName -eq 'Warehouse_Locations' | ForEach-Object Id)
$allLocations = @(Get-ApiData '/api/master/warehouse-locations' | Where-Object { [string]$_.id -in $locationIds })
Assert-Count $allLocations 14 'All seeded warehouse locations'
foreach ($location in $allLocations) {
    if (!$location.barcode) { throw 'A seeded warehouse location has no barcode.' }
    $lookup = Get-ApiData "/api/master/products/barcode-lookup?code=$($location.barcode)"
    if ($lookup.location.warehouseLocationId -ne $location.id) { throw 'Seeded location barcode lookup failed.' }
}
$preview = Invoke-WebRequest -Uri "$base/api/master/warehouse-locations/$($allLocations[0].id)/barcode-preview" -WebSession $session -SkipCertificateCheck -Headers @{'X-Company-Id'='1'}
if ($preview.StatusCode -ne 200 -or ($preview.Headers['Content-Type'] -join '') -notmatch 'image/svg\+xml') { throw 'Location barcode SVG preview failed.' }
Write-Output 'All location barcodes : PASS (14 codes, scan lookup and SVG preview)'

$documentTests = @(
    @{Table='PurchaseOrders';Path='purchase-orders';Number='purchaseOrderNo';Pattern='^PO-2026-\d{8}$';Count=46},
    @{Table='GoodReceipts';Path='good-receipts';Number='receiptNo';Pattern='^GRN-\d{6}$';Count=34},
    @{Table='PurchaseReturns';Path='purchase-returns';Number='returnNo';Pattern='^PR-2026-\d{8}$';Count=8},
    @{Table='StockIssues';Path='stock-issues';Number='issueNo';Pattern='^SI-2026-[A-F0-9]{8}$';Count=66}
)
foreach ($test in $documentTests) {
    $ids = @($manifest | Where-Object TableName -eq $test.Table | ForEach-Object Id)
    $data = Get-ApiData "/api/process/$($test.Path)?pageNumber=1&pageSize=100"
    $documents = @($data.items | Where-Object { [string]$_.id -in $ids })
    Assert-Count $documents $test.Count $test.Table
    foreach ($document in $documents) {
        if ($document.($test.Number) -notmatch $test.Pattern) { throw "Unexpected document format in $($test.Table)." }
    }
    # Check details for each newly received master PO/GRN and representative return/issue.
    $detailIds = @($manifest | Where-Object { $_.TableName -eq $test.Table -and $_.Batch -eq 'DemoMastersOrders20261023' } | ForEach-Object Id)
    if (!$detailIds.Count) { $detailIds = @($ids[0]) }
    foreach ($id in $detailIds) {
        $detail = Get-ApiData "/api/process/$($test.Path)/$id"
        if (!$detail -or $detail.($test.Number) -notmatch $test.Pattern) { throw 'Document detail mismatch.' }
    }
    Write-Output "$($test.Table) : PASS ($($documents.Count) default-format documents and detail checks)"
}
$lastReturn = Get-ApiData '/api/process/purchase-returns/last-number?year=2026'
if ($lastReturn -ne 'PR-2026-00000010') { throw 'Next-return numbering source mismatch.' }
Write-Output 'Return numbering : PASS (last 00000010; next default 00000011)'

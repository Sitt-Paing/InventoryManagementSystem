param(
    [Parameter(Mandatory = $true)][string]$Path
)

$ErrorActionPreference = 'Stop'
# Read the same local connection as the API without printing credentials.
$secretPath = Join-Path $env:APPDATA 'Microsoft/UserSecrets/49be33f5-73a8-46e3-913b-9f8a7d5988be/secrets.json'
$settings = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
$connectionString = $settings.'ConnectionStrings:DefaultConnection'
if ($env:ConnectionStrings__DefaultConnection) {
    $connectionString = $env:ConnectionStrings__DefaultConnection
}
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ($builder.DataSource -notin @('.', 'localhost', '(local)', '.\SQLEXPRESS', 'localhost\SQLEXPRESS') -or
    $builder.InitialCatalog -ne 'InventoryManagement') {
    throw 'This runner only supports the local InventoryManagement database.'
}
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    # Keep batch provenance outside human-facing audit columns.
    $manifestPath = Join-Path $PSScriptRoot 'demo-data-manifest.json'
    if (Test-Path -LiteralPath $manifestPath) {
        $manifestCommand = $connection.CreateCommand()
        $manifestCommand.CommandText = 'CREATE TABLE #DemoRecords(TableName nvarchar(128),Id nvarchar(50),Batch nvarchar(50),OriginalNumber nvarchar(50));'
        [void]$manifestCommand.ExecuteNonQuery()
        $manifestCommand.CommandText = @'
INSERT #DemoRecords SELECT TableName,Id,Batch,OriginalNumber FROM OPENJSON(@json)
WITH(TableName nvarchar(128),Id nvarchar(50),Batch nvarchar(50),OriginalNumber nvarchar(50));
'@
        [void]$manifestCommand.Parameters.Add('@json',[System.Data.SqlDbType]::NVarChar,-1)
        $manifestCommand.Parameters['@json'].Value = Get-Content -LiteralPath $manifestPath -Raw
        [void]$manifestCommand.ExecuteNonQuery()
    }
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 120
    $command.CommandText = Get-Content -LiteralPath $Path -Raw
    $adapter = [System.Data.SqlClient.SqlDataAdapter]::new($command)
    $results = [System.Data.DataSet]::new()
    [void]$adapter.Fill($results)
    foreach ($table in $results.Tables) {
        $table | Format-Table -AutoSize | Out-String -Width 220 | Write-Output
    }
}
finally {
    $connection.Dispose()
}

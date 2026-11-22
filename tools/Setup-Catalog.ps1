[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $DevelopmentRoot,
    [string] $Archive,
    [string] $Output,
    [switch] $CollisionGeometry,
    [string] $GeometryResources,
    [string] $GeometryConverter,
    [ValidateRange(1, 16)][int] $GeometryWorkers = 2
)

$ErrorActionPreference = 'Stop'
$client = (Resolve-Path -LiteralPath $DevelopmentRoot).ProviderPath
if (-not (Test-Path -LiteralPath (Join-Path $client '.eden-client-ready'))) {
    throw 'Stage build 3662168 with DevelopmentClient.ps1 before creating the catalog.'
}
$repo = Split-Path $PSScriptRoot -Parent
$nativeServer = Join-Path $repo 'build/windows-x64/server/Eden.Server.exe'
$portablePython = Join-Path $repo 'build/windows-x64/python/python.exe'
$python = if (Test-Path -LiteralPath $portablePython -PathType Leaf) { $portablePython } else { 'python' }
if ($Output) { $Output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output) }
Push-Location $repo
try {
    if (-not (Test-Path -LiteralPath $nativeServer -PathType Leaf)) {
        & dotnet build src/Eden.Server
        if ($LASTEXITCODE -ne 0) { throw 'The catalog verifier failed to build.' }
    }
    $arguments = @('tools/catalog/build.py', $client)
    if ($Archive) { $arguments += @('--archive', (Resolve-Path -LiteralPath $Archive).ProviderPath) }
    if ($Output) { $arguments += @('--output', $Output) }
    if (Test-Path -LiteralPath $nativeServer -PathType Leaf) { $arguments += @('--server-exe', $nativeServer) }
    & $python @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Catalog setup failed. See the source or verification error above.' }
    if ($CollisionGeometry) {
        $geometryCatalog = $Output
        if (-not $geometryCatalog) {
            $data = if ([string]::IsNullOrWhiteSpace($env:EDEN_DATA_DIR)) { 'data' } else { $env:EDEN_DATA_DIR }
            $geometryCatalog = Join-Path (Join-Path $repo 'src/Eden.Server') (Join-Path $data 'catalog.sqlite')
        }
        & "$PSScriptRoot/Export-StaticCollisionGeometry.ps1" -DevelopmentRoot $client -Catalog $geometryCatalog `
            -Resources $GeometryResources -Converter $GeometryConverter -Workers $GeometryWorkers
        & dotnet build src/Eden.Server --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Failed to embed the exported geometry in the server build.' }
    }
}
finally {
    Pop-Location
}

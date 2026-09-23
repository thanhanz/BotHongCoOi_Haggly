$ErrorActionPreference = 'Stop'

$backendDirectory = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $backendDirectory 'tools/Haggly.DataImport/Haggly.DataImport.csproj'
$packageDirectory = Join-Path $backendDirectory '.artifacts/tools'
$manifestPath = Join-Path $backendDirectory '.config/dotnet-tools.json'

dotnet pack $projectPath --configuration Release --output $packageDirectory
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool restore --tool-manifest $manifestPath --add-source $packageDirectory --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Installed. From backend/, run: dotnet haggly --help'

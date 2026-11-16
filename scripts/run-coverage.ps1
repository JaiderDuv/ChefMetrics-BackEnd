# Ejecuta pruebas y genera el reporte comparable con la medición inicial (7.123 líneas).
# Conserva además el XML sin filtros, incluyendo Contracts, Program y migraciones.
[CmdletBinding()]
param(
    [string]$ReportDirectory,
    [ValidateRange(0, 100)]
    [double]$MinimumLineCoverage = 70
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ReportDirectory)) {
    $ReportDirectory = Join-Path $projectRoot "TestResults/coveragereport"
}
$ReportDirectory = [System.IO.Path]::GetFullPath($ReportDirectory)
$reportTool = Get-Command reportgenerator -ErrorAction SilentlyContinue
if (-not $reportTool) {
    throw "Falta ReportGenerator. Instala: dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.6.0"
}

# Una carpeta nueva por ejecución evita sumar cobertura de ejecuciones anteriores.
$runDirectory = Join-Path $projectRoot ("TestResults/coverage-" + [guid]::NewGuid().ToString("N"))
Push-Location $projectRoot
try {
    dotnet test ChefMetrics-BackEnd.Tests/ChefMetrics-BackEnd.Tests.csproj `
        --configuration Debug -m:1 /nodeReuse:false `
        --logger "trx;LogFileName=test-results.trx" `
        --results-directory $runDirectory --collect:"XPlat Code Coverage"
    if ($LASTEXITCODE -ne 0) { throw "Las pruebas fallaron. Resultados: $runDirectory" }

    # El logger TRX puede duplicar el adjunto bajo su propia carpeta In.
    $coverageFiles = @(Get-ChildItem -LiteralPath $runDirectory -Directory |
        Where-Object { $_.Name -match '^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$' } |
        ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Filter coverage.cobertura.xml })
    if ($coverageFiles.Count -ne 1) { throw "Se esperaba un único XML de cobertura de esta ejecución." }
    $coverageFile = $coverageFiles[0].FullName

    & $reportTool.Source "-reports:$coverageFile" "-targetdir:$ReportDirectory" `
        "-reporttypes:Html_Dark;TextSummary;JsonSummary" `
        "-assemblyfilters:+ChefMetrics-BackEnd" `
        "-classfilters:-ChefMetrics_BackEnd.Migrations.*;-Program*"
    if ($LASTEXITCODE -ne 0) { throw "No se pudo generar el reporte HTML." }

    $summary = Get-Content -LiteralPath (Join-Path $ReportDirectory "Summary.json") -Raw | ConvertFrom-Json
    $covered = [int]$summary.summary.coveredlines
    $coverable = [int]$summary.summary.coverablelines
    if ($coverable -le 0) { throw "El reporte no contiene líneas medibles." }
    $percentage = 100.0 * $covered / $coverable
    Write-Host ("Cobertura de líneas del backend: {0:N2}% ({1}/{2})." -f $percentage, $covered, $coverable)
    Write-Host "Reporte HTML: $ReportDirectory/index.html"
    Write-Host "XML completo y TRX: $runDirectory"
    if ($percentage -lt $MinimumLineCoverage) {
        throw ("Cobertura {0:N2}% inferior al mínimo de {1}%." -f $percentage, $MinimumLineCoverage)
    }
}
finally {
    Pop-Location
}

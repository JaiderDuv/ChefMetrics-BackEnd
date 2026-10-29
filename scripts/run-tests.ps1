# Ejecuta las pruebas del backend y deja TRX + cobertura en TestResults.
# Uso: powershell -ExecutionPolicy Bypass -File scripts\run-tests.ps1
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet test ChefMetrics-BackEnd.sln `
        --logger "trx;LogFileName=test-results.trx" `
        --results-directory TestResults `
        --collect:"XPlat Code Coverage"

    if ($LASTEXITCODE -ne 0) {
        throw "Las pruebas fallaron. Revise TestResults\test-results.trx."
    }

    Write-Host ""
    Write-Host "Resultados disponibles en $root\TestResults" -ForegroundColor Green
}
finally {
    Pop-Location
}

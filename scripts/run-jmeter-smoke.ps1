param(
    [string]$ApiBaseUrl = "http://localhost:8000",
    [string]$JMeterHome = "tools\\apache-jmeter-5.6.3",
    [string]$Protocol = "https",
    [string]$Domain = "dummyjson.com",
    [int]$Threads = 2,
    [int]$Loops = 2
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$jmeterPath = Join-Path $workspaceRoot $JMeterHome
$jmeterBat = Join-Path $jmeterPath "bin\\jmeter.bat"
$testPlan = Join-Path $workspaceRoot "TestPlans\\HttpBin-Smoke.jmx"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runRoot = Join-Path $workspaceRoot "artifacts\\jmeter\\$timestamp"
$resultFile = Join-Path $runRoot "results.jtl"
$logFile = Join-Path $runRoot "jmeter.log"
$htmlReport = Join-Path $runRoot "html-report"

if (-not (Test-Path $jmeterBat)) {
    throw "No se encontro JMeter en $jmeterBat. Ejecuta primero scripts\\download-jmeter.ps1"
}

if (-not (Test-Path $testPlan)) {
    throw "No se encontro el plan JMeter en $testPlan"
}

New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

$jmeterArgs = @(
    "-n"
    "-t", $testPlan
    "-l", $resultFile
    "-j", $logFile
    "-e"
    "-o", $htmlReport
    "-Jthreads=$Threads"
    "-Jloops=$Loops"
    "-Jprotocol=$Protocol"
    "-Jdomain=$Domain"
    "-Jjmeter.save.saveservice.output_format=csv"
    "-Jjmeter.save.saveservice.assertion_results=none"
    "-Jjmeter.save.saveservice.response_data=false"
    "-Jjmeter.save.saveservice.samplerData=false"
    "-Jjmeter.save.saveservice.label=true"
    "-Jjmeter.save.saveservice.response_code=true"
    "-Jjmeter.save.saveservice.response_message=true"
    "-Jjmeter.save.saveservice.successful=true"
    "-Jjmeter.save.saveservice.thread_name=true"
    "-Jjmeter.save.saveservice.time=true"
    "-Jjmeter.save.saveservice.timestamp_format=ms"
)

Write-Host "Ejecutando JMeter..."
& $jmeterBat @jmeterArgs

Write-Host "Subiendo resultados a $ApiBaseUrl/api/performance/jmeter/upload"
& curl.exe -sS -X POST -F ("file=@" + $resultFile) "$ApiBaseUrl/api/performance/jmeter/upload" | Out-Host

Write-Host ""
Write-Host "Dashboard actualizado en $ApiBaseUrl/"
Write-Host "Target probado: ${Protocol}://$Domain"
Write-Host "Resultados JTL: $resultFile"
Write-Host "Reporte HTML de JMeter: $htmlReport\\index.html"

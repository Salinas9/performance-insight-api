param(
    [string]$Version = "5.6.3",
    [string]$InstallRoot = "tools"
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$installRootPath = Join-Path $workspaceRoot $InstallRoot
$targetDir = Join-Path $installRootPath "apache-jmeter-$Version"
$archivePath = Join-Path $installRootPath "apache-jmeter-$Version.zip"
$downloadUrl = "https://dlcdn.apache.org/jmeter/binaries/apache-jmeter-$Version.zip"

if (Test-Path $targetDir) {
    Write-Host "JMeter ya existe en $targetDir"
    exit 0
}

New-Item -ItemType Directory -Force -Path $installRootPath | Out-Null

Write-Host "Descargando Apache JMeter $Version desde $downloadUrl"
Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

Write-Host "Descomprimiendo en $installRootPath"
Expand-Archive -Path $archivePath -DestinationPath $installRootPath -Force
Remove-Item $archivePath -Force

Write-Host "JMeter instalado en $targetDir"

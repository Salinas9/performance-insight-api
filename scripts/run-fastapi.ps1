param(
    [string]$HostName = "127.0.0.1",
    [int]$Port = 8000
)

$ErrorActionPreference = "Stop"

$workspaceRoot = Split-Path -Parent $PSScriptRoot
$pythonExe = Join-Path $workspaceRoot ".venv\Scripts\python.exe"

if (-not (Test-Path $pythonExe)) {
    throw "No se encontro Python en $pythonExe. Ejecuta primero: python -m venv .venv y .\.venv\Scripts\python.exe -m pip install -r requirements.txt"
}

& $pythonExe -m uvicorn app.main:app --host $HostName --port $Port --reload
